using System;

namespace Tools {
	public class Noise {
		static Random rng = new Random();
		static double spareNormal;
		static bool spareNormalReady = false;
		//Box-Muller transform
		static double NormalValue {
			get {
				if (spareNormalReady) {
					spareNormalReady = false;
					return spareNormal;
				} else {
					double u = rng.NextDouble();
					double v = rng.NextDouble();
					while (u == 0) {
						u = rng.NextDouble();
					}

					spareNormalReady = true;
					spareNormal = Math.Sqrt(-2 * Math.Log(u)) * Math.Cos(Math.PI * 2 * v);
					return Math.Sqrt(-2 * Math.Log(u)) * Math.Sin(Math.PI * 2 * v);
				}
			}
		}

		//Set some values to the maximum or minimum
		public static void SaltAndPepper(byte[] image, double noisePercent, int bpp = -1, bool channelsTogether = false) {
			Random rng = new Random();

			if (channelsTogether) { //Per pixel
				for (int i = 0; i < image.Length / bpp; i++) {
					if (rng.NextDouble() < noisePercent) {
						int index = i * bpp;
						byte value = rng.NextDouble() < 0.5 ? (byte) 0 : (byte) 255;

						for (int b = 0; b < bpp; b++) {
							image[index + b] = value;
						}
					}
				}
			} else { //Per byte
				for (int i = 0; i < image.Length; i++) {
					if (rng.NextDouble() < noisePercent) {
						image[i] = rng.NextDouble() < 0.5 ? (byte) 0 : (byte) 255;
					}
				}
			}
		}

		//Set some values to random
		public static void Impulse(byte[] image, double noisePercent, int bpp = -1, bool channelsTogether = false) {
			Random rng = new Random();

			if (channelsTogether) { //Per pixel
				int length = image.Length / bpp;

				for (int i = 0; i < length; i++) {
					if (rng.NextDouble() < noisePercent) {
						int index = i * bpp;

						for (int b = 0; b < bpp; b++) {
							image[index + b] = (byte) rng.Next(256);
						}
					}
				}
			} else { //Per byte
				for (int i = 0; i < image.Length; i++) {
					if (rng.NextDouble() < noisePercent) {
						image[i] = (byte) rng.Next(256);
					}
				}
			}
		}

		//Multiply some values by a random number from 0 to 2; clamp
		public static void Speckle(byte[] image, double noisePercent, int bpp = -1, bool channelsTogether = false) {
			Random rng = new Random();

			if (channelsTogether) { //Per pixel
				int length = image.Length / bpp;

				for (int i = 0; i < length; i++) {
					if (rng.NextDouble() < noisePercent) {
						int index = i * bpp;
						double value = rng.NextDouble() * 2;

						for (int b = 0; b < bpp; b++) {
							image[index + b] = (byte) (Math.Min(image[index + b] * value, 255) + 0.5);
						}
					}
				}
			} else { //Per byte
				for (int i = 0; i < image.Length; i++) {
					if (rng.NextDouble() < noisePercent) {
						image[i] = (byte) (Math.Min(image[i] * rng.NextDouble() * 2, 255) + 0.5);
					}
				}
			}
		}

		//Add a normal distributed value to each value; clamp
		public static void Gaussian(byte[] image, double std, int bpp = -1, bool channelsTogether = false) {
			Random rng = new Random();

			if (channelsTogether) { //Per pixel
				int length = image.Length / bpp;

				for (int i = 0; i < length; i++) {
					int index = i * bpp;
					double value = NormalValue * std;

					for (int b = 0; b < bpp; b++) {
						image[index + b] = (byte) (Math.Max(Math.Min(image[index + b] + value, 255), 0) + 0.5);
					}
				}
			} else { //Per byte
				for (int i = 0; i < image.Length; i++) {
					image[i] = (byte) (Math.Max(Math.Min(image[i] + NormalValue * std, 255), 0) + 0.5);
				}
			}
		}

		//Add a Poisson distributed value to each value; clamp
		public static void Poisson(byte[] image, double averagePhotons, int bpp = -1, bool channelsTogether = false) {
			Random rng = new Random();

			//Knuth's method
			double PoissonValue() {
				int photons = 0;
				double prob = rng.NextDouble();
				double upper = Math.Exp(-averagePhotons);
				while (prob > upper) {
					photons++;
					prob *= rng.NextDouble();
				}
				return photons / averagePhotons;
			}

			if (channelsTogether) { //Per pixel
				int length = image.Length / bpp;

				for (int i = 0; i < length; i++) {
					int index = i * bpp;
					double value = PoissonValue();

					for (int b = 0; b < bpp; b++) {
						image[index + b] = (byte) (Math.Min(image[index + b] * value, 255) + 0.5);
					}
				}
			} else { //Per byte
				for (int i = 0; i < image.Length; i++) {
					image[i] = (byte) (Math.Min(image[i] * PoissonValue(), 255) + 0.5);
				}
			}
		}
	}
}
