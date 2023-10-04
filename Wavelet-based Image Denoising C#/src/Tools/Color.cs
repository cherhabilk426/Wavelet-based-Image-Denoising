using System;
using System.Threading.Tasks;

namespace Tools {
	public class Color {
		public static float[] ToHSV(byte[] image, bool skipAlpha) {
			float[] newImage = new float[image.Length];
			if (skipAlpha) {
				Parallel.For(0, image.Length / 4, i => {
					int index = i * 4;
					ToHSV(image[index], image[index + 1], image[index + 2], out newImage[index], out newImage[index + 1], out newImage[index + 2]);
					newImage[index + 3] = image[index + 3] / 255f;
				});
			} else {
				Parallel.For(0, image.Length / 3, i => {
					int index = i * 3;
					ToHSV(image[index], image[index + 1], image[index + 2], out newImage[index], out newImage[index + 1], out newImage[index + 2]);
				});
			}
			return newImage;
		}

		public static float[] ToHSL(byte[] image, bool skipAlpha) {
			float[] newImage = new float[image.Length];
			if (skipAlpha) {
				Parallel.For(0, image.Length / 4, i => {
					int index = i * 4;
					ToHSL(image[index], image[index + 1], image[index + 2], out newImage[index], out newImage[index + 1], out newImage[index + 2]);
					newImage[index + 3] = image[index + 3] / 255f;
				});
			} else {
				Parallel.For(0, image.Length / 3, i => {
					int index = i * 3;
					ToHSL(image[index], image[index + 1], image[index + 2], out newImage[index], out newImage[index + 1], out newImage[index + 2]);
				});
			}
			return newImage;
		}

		public static byte[] FromHSV(float[] image, bool skipAlpha) {
			byte[] newImage = new byte[image.Length];
			if (skipAlpha) {
				Parallel.For(0, image.Length / 4, i => {
					int index = i * 4;
					FromHSV(image[index], image[index + 1], image[index + 2], out newImage[index], out newImage[index + 1], out newImage[index + 2]);
					newImage[index + 3] = (byte) (image[index + 3] * 255 + 0.5f);
				});
			} else {
				Parallel.For(0, image.Length / 3, i => {
					int index = i * 3;
					FromHSV(image[index], image[index + 1], image[index + 2], out newImage[index], out newImage[index + 1], out newImage[index + 2]);
				});
			}
			return newImage;
		}

		public static byte[] FromHSL(float[] image, bool skipAlpha) {
			byte[] newImage = new byte[image.Length];
			if (skipAlpha) {
				Parallel.For(0, image.Length / 4, i => {
					int index = i * 4;
					FromHSL(image[index], image[index + 1], image[index + 2], out newImage[index], out newImage[index + 1], out newImage[index + 2]);
					newImage[index + 3] = (byte) (image[index + 3] * 255 + 0.5f);
				});
			} else {
				Parallel.For(0, image.Length / 3, i => {
					int index = i * 3;
					FromHSL(image[index], image[index + 1], image[index + 2], out newImage[index], out newImage[index + 1], out newImage[index + 2]);
				});
			}
			return newImage;
		}

		static void ToChromaHue(float r, float g, float b, out float max, out float min, out float chroma, out float hue) {
			//Operating in [0, 1] range
			r /= 255;
			g /= 255;
			b /= 255;

			max = Math.Max(r, Math.Max(g, b));
			min = Math.Min(r, Math.Min(g, b));

			chroma = max - min;
			if (chroma == 0) {
				hue = 0;
			} else if (max == r) {
				hue = ((g - b) / chroma + 6) % 6;
			} else if (max == g) {
				hue = (b - r) / chroma + 2;
			} else {
				hue = (r - g) / chroma + 4;
			}
			hue /= 6;
		}

		static void FromChromaHue(float min, float chroma, float hue, out byte red, out byte green, out byte blue) {
			float r = 0, g = 0, b = 0;

			hue *= 6;
			float x = chroma * (1 - Math.Abs(hue % 2 - 1));
			if (hue <= 1) {
				r = chroma;
				g = x;
			} else if (hue <= 2) {
				r = x;
				g = chroma;
			} else if (hue <= 3) {
				g = chroma;
				b = x;
			} else if (hue <= 4) {
				g = x;
				b = chroma;
			} else if (hue <= 5) {
				r = x;
				b = chroma;
			} else {
				r = chroma;
				b = x;
			}

			r += min;
			g += min;
			b += min;

			red = (byte) (r * 255 + 0.5f);
			green = (byte) (g * 255 + 0.5f);
			blue = (byte) (b * 255 + 0.5f);
		}

		static void ToHSV(byte r, byte g, byte b, out float hue, out float saturation, out float value) {
			ToChromaHue(r, g, b, out value, out float min, out float chroma, out hue);
			saturation = chroma == 0 ? 0 : chroma / value;
		}

		static void ToHSL(byte r, byte g, byte b, out float hue, out float saturation, out float lightness) {
			ToChromaHue(r, g, b, out float max, out float min, out float chroma, out hue);
			lightness = (max + min) / 2;
			saturation = chroma == 0 ? 0 : chroma / (1 - Math.Abs(2 * lightness - 1));
		}

		static void FromHSV(float hue, float saturation, float value, out byte red, out byte green, out byte blue) {
			float chroma = value * saturation;
			FromChromaHue(value - chroma, chroma, hue, out red, out green, out blue);
		}

		static void FromHSL(float hue, float saturation, float lightness, out byte red, out byte green, out byte blue) {
			float chroma = (1 - Math.Abs(2 * lightness - 1)) * saturation;
			FromChromaHue(lightness - chroma / 2, chroma, hue, out red, out green, out blue);
		}

		public static float[] ToYUV(byte[] image, bool skipAlpha) {
			float[] newImage = new float[image.Length];
			if (skipAlpha) {
				Parallel.For(0, image.Length / 4, i => {
					int index = i * 4;
					byte b = image[index];
					byte g = image[index + 1];
					byte r = image[index + 2];
					float y = 0.299f * r + 0.587f * g + 0.114f * b;
					newImage[index] = y;
					newImage[index + 1] = (b - y) / 1.772f + 127.5f; //u
					newImage[index + 2] = (r - y) / 1.402f + 127.5f; //v
					newImage[index + 3] = image[index + 3];
				});
			} else {
				Parallel.For(0, image.Length / 3, i => {
					int index = i * 3;
					byte b = image[index];
					byte g = image[index + 1];
					byte r = image[index + 2];
					float y = 0.299f * r + 0.587f * g + 0.114f * b;
					newImage[index] = y;
					newImage[index + 1] = (b - y) / 1.772f + 127.5f; //u
					newImage[index + 2] = (r - y) / 1.402f + 127.5f; //v
				});
			}
			return newImage;
		}

		public static byte[] FromYUV(float[] image, bool skipAlpha) {
			byte[] newImage = new byte[image.Length];
			if (skipAlpha) {
				Parallel.For(0, image.Length / 4, i => {
					int index = i * 4;
					float y = image[index];
					float u = image[index + 1] - 127.5f;
					float v = image[index + 2] - 127.5f;
					newImage[index] = (byte) (Math.Min(Math.Max(y + 1.772f * u, 0), 255) + 0.5f); //b
					newImage[index + 1] = (byte) (Math.Min(Math.Max(y - 0.344136f * u - 0.714136f * v, 0), 255) + 0.5f); //g
					newImage[index + 2] = (byte) (Math.Min(Math.Max(y + 1.402f * v, 0), 255) + 0.5f); //r
					newImage[index + 3] = (byte) (image[index + 3] + 0.5f);
				});
			} else {
				Parallel.For(0, image.Length / 3, i => {
					int index = i * 3;
					float y = image[index];
					float u = image[index + 1] - 127.5f;
					float v = image[index + 2] - 127.5f;
					newImage[index] = (byte) (Math.Min(Math.Max(y + 1.772f * u, 0), 255) + 0.5f); //b
					newImage[index + 1] = (byte) (Math.Min(Math.Max(y - 0.344136f * u - 0.714136f * v , 0), 255) + 0.5f); //g
					newImage[index + 2] = (byte) (Math.Min(Math.Max(y + 1.402f * v, 0), 255) + 0.5f); //r
				});
			}
			return newImage;
		}
	}
}
