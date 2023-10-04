using System;
using System.Threading.Tasks;
using System.Drawing.Imaging;
using System.Collections.Generic;

class Denoiser {
	static void Main(string[] args) {
		//Get file data
		string inPath = args.Length > 0 ? args[0] : Console.ReadLine();
		int extensionIndex = inPath.LastIndexOf('.');
		if (extensionIndex == -1) {
			extensionIndex = inPath.Length;
		}
		string fileName = inPath.Substring(0, extensionIndex);
		byte[] inImage = Tools.IO.LoadImage(inPath, out int width, out int height, PixelFormat.Format24bppRgb);
		byte[] outImage = new byte[inImage.Length];

		//Gather processing details
		Console.WriteLine("Enter transform level:");
		int level = int.Parse(args.Length > 1 ? args[1] : Console.ReadLine());
		if (width % Power(2, level) != 0 || height % Power(2, level) != 0) {
			Console.WriteLine("Width or height has to be a multiple of the transform level power of two.");
			return;
		}

		//Ask user for thresholding multiplier and quantization steps
		Console.WriteLine("Enter thresholding multiplier: [0.0 - 3.0]");
		float thresholdMult = float.Parse(args.Length > 2 ? args[2] : Console.ReadLine());
		Console.WriteLine("Enter quantization step size (smaller is less compressed):");
		float stepSize = int.Parse(args.Length > 3 ? args[3] : Console.ReadLine());

		//Reference entropy before processing
		//Console.WriteLine(ComputeCompressedSize(Array.ConvertAll(inImage, x => (float) x), width, height, 0));

		//Colorspace conversion
		float[][] yuv = new float[3][] { new float[inImage.Length / 3], new float[inImage.Length / 3], new float[inImage.Length / 3] };
		ToYUV(inImage, yuv);
		/*{ //Save YUV for presentation/debugging
			float[] black = new float[yuv[0].Length];
			float[] gray = Array.ConvertAll(yuv[0], x => 127.5f);
			SaveYUV(new float[3][] { yuv[0], gray, gray }, outImage, fileName + " y.png", width, height, 0);
			SaveYUV(new float[3][] { black, yuv[1], gray }, outImage, fileName + " u.png", width, height, 0);
			SaveYUV(new float[3][] { black, gray, yuv[2] }, outImage, fileName + " v.png", width, height, 0);
		}*/

		//Do the whole process with different transforms
		Process1(new float[3][] { (float[]) yuv[0].Clone(), (float[]) yuv[1].Clone(), (float[]) yuv[2].Clone() }, outImage, width, height, level, fileName + " Haar" + level, Haar, HaarInv, thresholdMult, stepSize);
		Process1(new float[3][] { (float[]) yuv[0].Clone(), (float[]) yuv[1].Clone(), (float[]) yuv[2].Clone() }, outImage, width, height, level, fileName + " Orth" + level, Orth, OrthInv, thresholdMult, stepSize);
	}

	static int Power(int multiplier, int power, int value = 1) {
		return power == 0 ? value : Power(multiplier, power - 1, value * multiplier);
	}

	static void SaveYUV(float[][] yuv, byte[] outImage, string path, int width, int height, int level) {
		float[][] newYUV = new float[3][] { new float[yuv[0].Length], new float[yuv[1].Length], new float[yuv[2].Length] };
		//Take absolute values of high-pass components
		int lowPassWidth = width / Power(2, level);
		int lowPassHeight = height / Power(2, level);
		Parallel.For(0, height, y => {
			int yIndex = y * width;
			for (int x = 0; x < width; x++) {
				bool isLowPass = x < lowPassWidth && y < lowPassHeight;
				int i = yIndex + x;

				newYUV[0][i] = isLowPass ? yuv[0][i] : Math.Abs(yuv[0][i]) * 2;
				newYUV[1][i] = isLowPass ? yuv[1][i] : 127.5f + yuv[1][i];
				newYUV[2][i] = isLowPass ? yuv[2][i] : 127.5f + yuv[2][i];
			}
		});
		//Convert back to RGB and save
		FromYUV(outImage, newYUV);
		Tools.IO.SaveImage(outImage, path, width, height, PixelFormat.Format24bppRgb);
	}

	#region Separating YCbCr transforms
	static void ToYUV(byte[] inImage, float[][] image) {
		Parallel.For(0, inImage.Length / 3, i => {
			int index = i * 3;
			byte b = inImage[index];
			byte g = inImage[index + 1];
			byte r = inImage[index + 2];
			float y = 0.299f * r + 0.587f * g + 0.114f * b;
			image[0][i] = y;
			image[1][i] = (b - y) / 1.772f + 127.5f; //u
			image[2][i] = (r - y) / 1.402f + 127.5f; //v
		});
	}

	static void FromYUV(byte[] outImage, float[][] image) {
		Parallel.For(0, image[0].Length, i => {
			int index = i * 3;
			float y = image[0][i];
			float u = image[1][i] - 127.5f;
			float v = image[2][i] - 127.5f;
			outImage[index] = (byte) (Math.Min(Math.Max(y + 1.772f * u, 0), 255) + 0.5f); //b
			outImage[index + 1] = (byte) (Math.Min(Math.Max(y - 0.344136f * u - 0.714136f * v, 0), 255) + 0.5f); //g
			outImage[index + 2] = (byte) (Math.Min(Math.Max(y + 1.402f * v, 0), 255) + 0.5f); //r
		});
	}
	#endregion

	#region Wavelet transforms
	static void Haar(float[] image, int width, int height, int stride) {
		float[] intermediateImage = new float[image.Length];

		//Horizontal processing
		int halfWidth = width / 2;
		Parallel.For(0, height, y => {
			int yIndex = y * stride;

			for (int x = 0; x < halfWidth; x++) {
				int targetIndex = yIndex + x;
				int sourceIndex = targetIndex + x;

				float source = image[sourceIndex];
				float source2 = image[sourceIndex + 1];
				intermediateImage[targetIndex] = (source + source2) / 2; //Sum
				intermediateImage[targetIndex + halfWidth] = (source - source2) / 2; //Difference
			}
		});

		//Vertical processing
		int halfHeight = height / 2;
		int halfHeightIndex = halfHeight * stride;
		Parallel.For(0, halfHeight, y => {
			int yIndex = y * stride;

			for (int x = 0; x < width; x++) {
				int targetIndex = yIndex + x;
				int sourceIndex = targetIndex + yIndex;

				float source = intermediateImage[sourceIndex];
				float source2 = intermediateImage[sourceIndex + stride];
				image[targetIndex] = (source + source2) / 2; //Sum
				image[targetIndex + halfHeightIndex] = (source - source2) / 2; //Difference
			}
		});
	}

	static void HaarInv(float[] image, int width, int height, int stride) {
		float[] intermediateImage = new float[image.Length];

		//Horizontal processing
		Parallel.For(0, height * 2, y => {
			int yIndex = y * stride;

			for (int x = 0; x < width; x++) {
				int sourceIndex = yIndex + x;
				int targetIndex = sourceIndex + x;

				float source = image[sourceIndex];
				float source2 = image[sourceIndex + width];
				intermediateImage[targetIndex] = source + source2;
				intermediateImage[targetIndex + 1] = source - source2;
			}
		});

		//Vertical processing
		int doubleWidth = width * 2;
		int heightIndex = height * stride;
		Parallel.For(0, height, y => {
			int yIndex = y * stride;

			for (int x = 0; x < doubleWidth; x++) {
				int sourceIndex = yIndex + x;
				int targetIndex = sourceIndex + yIndex;

				float source = intermediateImage[sourceIndex];
				float source2 = intermediateImage[sourceIndex + heightIndex];
				image[targetIndex] = source + source2;
				image[targetIndex + stride] = source - source2;
			}
		});
	}

	static void Orth(float[] image, int width, int height, int stride) {
		//Note: Sqrt(50) is the "proper" divisor. Overall the image has to be divded by 2500 - 50 in each dimension across a back-forth transform.
		float[] lowPassFilter = { 0.2f, 0.6f, 0.3f, -0.1f };
		float[] highPassFilter = { 0.1f, 0.3f, -0.6f, 0.2f };

		float[] intermediateImage = new float[image.Length];

		//Horizontal processing
		int halfWidth = width / 2;
		Parallel.For(0, height, y => {
			int yIndex = y * stride;

			for (int x = 0; x < halfWidth; x++) {
				int targetIndexLow = yIndex + x;
				int sourceIndex = targetIndexLow + x;

				float lowPass = 0;
				float highPass = 0;

				for (int i = 0; i < 4; i++) {
					//For samples that would go past the end of the image, instead sample the beginning of the image.
					float source = image[(x == halfWidth - 1 && i > 1) ? (yIndex + i - 2) : (sourceIndex + i)];
					lowPass += source * lowPassFilter[i];
					highPass += source * highPassFilter[i];
				}

				intermediateImage[targetIndexLow] = lowPass;
				intermediateImage[targetIndexLow + halfWidth] = highPass;
			}
		});

		//Vertical processing
		int halfHeight = height / 2;
		int halfHeightIndex = halfHeight * stride;
		Parallel.For(0, halfHeight, y => {
			int yIndex = y * stride;

			for (int x = 0; x < width; x++) {
				int targetIndexLow = yIndex + x;
				int sourceIndex = targetIndexLow + yIndex;

				float lowPass = 0;
				float highPass = 0;

				for (int i = 0; i < 4; i++) {
					//For samples that would go past the end of the image, instead sample the beginning of the image.
					float source = intermediateImage[(y == halfHeight - 1 && i > 1) ? ((i - 2) * stride + x) : (sourceIndex + i * stride)];
					lowPass += source * lowPassFilter[i];
					highPass += source * highPassFilter[i];
				}

				image[targetIndexLow] = lowPass;
				image[targetIndexLow + halfHeightIndex] = highPass;
			}
		});
	}

	static void OrthInv(float[] image, int width, int height, int stride) {
		float[] reconstruction = { 0.6f, -1.2f, 0.4f, 0.2f };
		float[] reconstruction2 = { -0.2f, 0.4f, 1.2f, 0.6f };

		float[] intermediateImage = new float[image.Length];

		//Horizontal processing
		Parallel.For(0, height * 2, y => {
			int yIndex = y * stride;

			for (int x = 0; x < width; x++) {
				int sourceIndexLow = yIndex + x;
				int sourceIndexHigh = sourceIndexLow + width;
				int targetIndex = sourceIndexLow + x;

				float target = 0;
				float target2 = 0;

				for (int i = 0; i < 4; i++) {
					//For samples that would go past the beginning of the image, instead sample the end of the image.
					float source = image[(x == 0 && i < 2) ?
						(yIndex + (i % 2 == 0 ? width - 1 : width - 1 + width)) :
						((i % 2 == 0 ? sourceIndexLow : sourceIndexHigh) + i / 2 - 1)];
					target += source * reconstruction[i];
					target2 += source * reconstruction2[i];
				}

				intermediateImage[targetIndex] = target;
				intermediateImage[targetIndex + 1] = target2;
			}
		});

		//Vertical processing
		int doubleWidth = width * 2;
		int heightIndex = height * stride;
		Parallel.For(0, height, y => {
			int yIndex = y * stride;

			for (int x = 0; x < doubleWidth; x++) {
				int sourceIndexLow = yIndex + x;
				int sourceIndexHigh = sourceIndexLow + heightIndex;
				int targetIndex = sourceIndexLow + yIndex;

				float target = 0;
				float target2 = 0;

				for (int i = 0; i < 4; i++) {
					//For samples that would go past the beginning of the image, instead sample the end of the image.
					float source = intermediateImage[(y == 0 && i < 2) ?
						(i % 2 == 0 ? (height - 1) * stride : (height - 1) * stride + heightIndex) + x :
						((i % 2 == 0 ? sourceIndexLow : sourceIndexHigh) + (i / 2 - 1) * stride)];
					target += source * reconstruction[i];
					target2 += source * reconstruction2[i];
				}

				image[targetIndex] = target;
				image[targetIndex + stride] = target2;
			}
		});
	}
	#endregion

	#region Denoising
	static float FindMedian(float[] image, int width, int height) {
		int halfWidth = width / 2;
		int halfHeight = height / 2;
		int medianIndex = halfWidth * halfHeight / 2;
		List<float> smaller = new List<float>();
		List<float> larger = new List<float>();
		float pivot = Math.Abs(image[halfHeight * width + halfWidth]);
		//First pass
		for (int y = halfHeight; y < height; y++) {
			int yIndex = y * width;
			for (int x = halfWidth; x < width; x++) {
				int i = yIndex + x;
				float value = Math.Abs(image[i]);
				if (value <= pivot) {
					smaller.Add(value);
				} else {
					larger.Add(value);
				}
			}
		}
		List<float> old;
		if (medianIndex < smaller.Count) {
			old = new List<float>(smaller);
		} else {
			medianIndex -= smaller.Count;
			old = new List<float>(larger);
		}
		int lastCount = 0;
		//Rest of the passes
		while (old.Count > 1 && lastCount != old.Count) { //Iterate until only 1 element is left or all elements are the same
			lastCount = old.Count;
			smaller.Clear();
			larger.Clear();
			pivot = old[0];
			foreach (float value in old) {
				if (value <= pivot) {
					smaller.Add(value);
				} else {
					larger.Add(value);
				}
			}
			if (medianIndex < smaller.Count) {
				old = new List<float>(smaller);
			} else {
				medianIndex -= smaller.Count;
				old = new List<float>(larger);
			}
		}
		return old[0];
	}

	static void Threshold(float[] image, int width, int height, float noise, int windows, Func<float, float, float> func) {
		for (int s = 0; s < windows; s++) {
			int level = Power(2, s / 3);
			int rightSide = width / level;
			int downSide = height / level;

			switch (s % 3) {
				case 0: //Bottom right
					Threshold(image, rightSide / 2, downSide / 2, rightSide, downSide, width, noise, level, func);
					break;
				case 1: //Bottom left
					Threshold(image, 0, downSide / 2, rightSide / 2, downSide, width, noise, level, func);
					break;
				case 2: //Top right
					Threshold(image, rightSide / 2, 0, rightSide, downSide / 2, width, noise, level, func);
					break;
			}
		}
	}

	static void Threshold(float[] image, int x1, int y1, int x2, int y2, int stride, float noise, int level, Func<float, float, float> func) {
		//Find subband threshold
		float threshold = 0;
		for (int y = y1; y < y2; y++) {
			int yIndex = y * stride;

			for (int x = x1; x < x2; x++) {
				int i = yIndex + x;
				threshold += image[i] * image[i];
			}
		}
		//BayesShrink
		//Level makes less information be removed from higher levels, to compensate for normalized color values
		threshold = (float) (noise / Math.Sqrt(Math.Max(level * level * threshold / ((x2 - x1) * (y2 - y1)) - noise, 0)));

		//Threshold image
		for (int y = y1; y < y2; y++) {
			int yIndex = y * stride;

			for (int x = x1; x < x2; x++) {
				int i = yIndex + x;
				image[i] = func(image[i], threshold);
			}
		}
	}
	#endregion

	#region Compression
	static void Quantize(float[] image, int width, int height, float stepSize, int windows) {
		int level = 1;
		Parallel.For(0, windows, s => {
			level = Power(2, s / 3);
			int rightSide = width / level;
			int downSide = height / level;
			float step = stepSize / level;

			switch (s % 3) {
				case 0: //Bottom right
					Quantize(image, rightSide / 2, downSide / 2, rightSide, downSide, width, step);
					break;
				case 1: //Bottom left
					Quantize(image, 0, downSide / 2, rightSide / 2, downSide, width, step);
					break;
				case 2: //Top right
					Quantize(image, rightSide / 2, 0, rightSide, downSide / 2, width, step);
					break;
			}
		});

		//Top left
		level *= 2;
		Quantize(image, 0, 0, width / level, height / level, width, stepSize / level);
	}

	static void Quantize(float[] image, int x1, int y1, int x2, int y2, int stride, float step) {
		for (int y = y1; y < y2; y++) {
			int yIndex = y * stride;

			for (int x = x1; x < x2; x++) {
				int i = yIndex + x;

				image[i] = Convert.ToInt32(image[i] / step) * step;
			}
		}
	}

	static double ComputeCompressedSize(float[] image, int width, int height, int windows) {
		double sum = 0;

		for (int s = 0; s < windows; s++) {
			int divisor = Power(2, s / 3);
			int rightSide = width / divisor;
			int downSide = height / divisor;

			switch (s % 3) {
				case 0: //Bottom right
					sum += ComputeCompressedSize(image, rightSide / 2, downSide / 2, rightSide, downSide, width);
					break;
				case 1: //Bottom left
					sum += ComputeCompressedSize(image, 0, downSide / 2, rightSide / 2, downSide, width);
					break;
				case 2: //Top right
					sum += ComputeCompressedSize(image, rightSide / 2, 0, rightSide, downSide / 2, width);
					break;
			}
		}

		//Top left
		int divisor2 = Power(2, windows / 3);
		sum += ComputeCompressedSize(image, 0, 0, width / divisor2, height / divisor2, width);

		return sum / image.Length;
	}

	static double ComputeCompressedSize(float[] image, int x1, int y1, int x2, int y2, int stride) {
		double sum = 0;
		var occurances = new Dictionary<float, int>();

		for (int y = y1; y < y2; y++) {
			int yIndex = y * stride;

			for (int x = x1; x < x2; x++) {
				int i = yIndex + x;

				if (occurances.TryGetValue(image[i], out int count)) {
					occurances[image[i]] = count + 1;
				} else {
					occurances[image[i]] = 1;
				}
			}
		}

		int size = (x2 - x1) * (y2 - y1);
		foreach (int value in occurances.Values) {
			sum -= value * Math.Log((double) value / size, 2);
		}
		return sum;
	}
	#endregion

	static void Process1(float[][] yuv, byte[] outImage, int width, int height, int level, string fileName, Action<float[], int, int, int> transform, Action<float[], int, int, int> inverseTransform, float thresholdMult, float stepSize) {
		//Do the transform
		int newWidth = width;
		int newHeight = height;
		for (int i = 0; i < level; i++) {
			for (int c = 0; c < 3; c++) {
				transform(yuv[c], newWidth, newHeight, width);
			}
			newWidth /= 2;
			newHeight /= 2;
		}
		//SaveYUV(yuv, outImage, fileName + ".png", width, height, level); //Save

		//Find medians for thresholding
		float[] medians = new float[3] { FindMedian(yuv[0], width, height), FindMedian(yuv[1], width, height), FindMedian(yuv[2], width, height) };

		//Do the rest of the process with different types of thresholding
		Process2(new float[3][] { (float[]) yuv[0].Clone(), (float[]) yuv[1].Clone(), (float[]) yuv[2].Clone() }, outImage, width, height, newWidth, newHeight, level, fileName + " Soft" + thresholdMult, inverseTransform, (x, value) => (x > value) ? (x - value) : ((x < -value) ? (x + value) : 0), thresholdMult, stepSize, medians);
		Process2(new float[3][] { (float[]) yuv[0].Clone(), (float[]) yuv[1].Clone(), (float[]) yuv[2].Clone() }, outImage, width, height, newWidth, newHeight, level, fileName + " Hard" + thresholdMult, inverseTransform, (x, value) => (Math.Abs(x) > value) ? x : 0, thresholdMult, stepSize, medians);
		Process2(new float[3][] { (float[]) yuv[0].Clone(), (float[]) yuv[1].Clone(), (float[]) yuv[2].Clone() }, outImage, width, height, newWidth, newHeight, level, fileName + " Medium" + thresholdMult, inverseTransform, (x, value) => (Math.Abs(x) > value) ? 127.5f * (x > 0 ? x - value : x + value) / (127.5f - value) : 0, thresholdMult, stepSize, medians);
	}

	static void Process2(float[][] yuv, byte[] outImage, int width, int height, int newWidth, int newHeight, int level, string fileName, Action<float[], int, int, int> inverseTransform, Func<float, float, float> thresholdFun, float thresholdMult, float stepSize, float[] medians) {
		//Threshold
		if (thresholdMult > 1e-4f) {
			for (int c = 0; c < 3; c++) {
				float noise = medians[c] * thresholdMult;
				Threshold(yuv[c], width, height, noise * noise, level * 3, thresholdFun);
			}
		}
		//SaveYUV(yuv, outImage, fileName + ".png", width, height, level); //Save

		//Quantize and measure compression
		if (stepSize > 1e-4f) {
			for (int c = 0; c < 3; c++) {
				Quantize(yuv[c], width, height, stepSize, level * 3);
			}
			double yComp = ComputeCompressedSize(yuv[0], width, height, level * 3);
			double uComp = ComputeCompressedSize(yuv[1], width, height, level * 3);
			double vComp = ComputeCompressedSize(yuv[2], width, height, level * 3);
			Console.WriteLine(yComp + "; " + uComp + "; " + vComp);
			Console.WriteLine((yComp + uComp + vComp) / 3);
			//SaveYUV(yuv, outImage, fileName + " Quantized" + stepSize + ".png", width, height, level * 3); //Save
		}

		//Do the inverse transform
		for (int i = 0; i < level; i++) {
			for (int c = 0; c < 3; c++) {
				inverseTransform(yuv[c], newWidth, newHeight, width);
			}
			newWidth *= 2;
			newHeight *= 2;
		}

		//Backwards colorspace conversion, save the output
		FromYUV(outImage, yuv);
		Tools.IO.SaveImage(outImage, fileName + " Denoised.png", width, height, PixelFormat.Format24bppRgb);
	}
}
