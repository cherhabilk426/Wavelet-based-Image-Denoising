using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

//TODO: Images loading of the wrong pixel size?

namespace Tools {
	public class IO {
		public static byte[] LoadImage(string path, out int width, out int height, ref PixelFormat pixelFormat) {
			Bitmap original = new Bitmap(path);
			if (pixelFormat == PixelFormat.DontCare) {
				pixelFormat = original.PixelFormat;
			}
			width = original.Width;
			height = original.Height;
			byte[] image;

			if (original.PixelFormat == pixelFormat) {
				BitmapData bitmapData = original.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, pixelFormat);
				image = new byte[bitmapData.Stride * height];
				Marshal.Copy(bitmapData.Scan0, image, 0, image.Length);
				original.UnlockBits(bitmapData);
				original.Dispose();
			} else {
				Bitmap bitmap = original.Clone(new Rectangle(0, 0, width, height), pixelFormat);
				original.Dispose();
				BitmapData bitmapData = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, pixelFormat);
				image = new byte[bitmapData.Stride * height];
				Marshal.Copy(bitmapData.Scan0, image, 0, image.Length);
				bitmap.UnlockBits(bitmapData);
				bitmap.Dispose();
			}

			if (width * height * Image.GetPixelFormatSize(pixelFormat) != image.Length * 8) {
				image = CorrectStride(image, width * Image.GetPixelFormatSize(pixelFormat) / 8, height, true);
			}
			return image;
		}

		public static byte[] LoadImage(string path, out int width, out int height, PixelFormat pixelFormat) {
			return LoadImage(path, out width, out height, ref pixelFormat);
		}

		public static void SaveImage(byte[] image, string path, int width, int height, PixelFormat pixelFormat) {
			if (width * Image.GetPixelFormatSize(pixelFormat) % 32 != 0) {
				image = CorrectStride(image, width * Image.GetPixelFormatSize(pixelFormat) / 8, height, false);
			}

			Bitmap bitmap = new Bitmap(width, height, pixelFormat);
			BitmapData bitmapData = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, pixelFormat);
			Marshal.Copy(image, 0, bitmapData.Scan0, image.Length);
			bitmap.UnlockBits(bitmapData);
			bitmap.Save(path);
			bitmap.Dispose();
		}

		static byte[] CorrectStride(byte[] oldImage, int rowLength, int height, bool removing) {
			if (removing) {
				int stride = oldImage.Length / height;
				byte[] image = new byte[rowLength * height];

				Parallel.For(0, height, row => Array.Copy(oldImage, row * stride, image, row * rowLength, rowLength));
				return image;
			} else {
				int stride = (rowLength + 3) / 4 * 4;
				byte[] image = new byte[stride * height];

				Parallel.For(0, height, row => Array.Copy(oldImage, row * rowLength, image, row * stride, rowLength));
				return image;
			}
		}

		//https://en.wikipedia.org/wiki/SRGB
		public static float[] GammaDecode(byte[] image, bool skipAlpha) {
			float[] newImage = new float[image.Length];
			if (skipAlpha) {
				Parallel.For(0, image.Length, i => newImage[i] = i % 4 == 3 ? image[i] / 255f : (float) (image[i] <= 10 ? image[i] / 3295.418601 : Math.Pow((image[i] + 14.025) / 269.025, 2.4)));
			} else {
				Parallel.For(0, image.Length, i => newImage[i] = (float) (image[i] <= 10 ? image[i] / 3295.418601 : Math.Pow((image[i] + 14.025) / 269.025, 2.4)));
			}
			return newImage;
		}

		public static byte[] GammaEncode(float[] image, bool skipAlpha) {
			byte[] newImage = new byte[image.Length];
			if (skipAlpha) {
				Parallel.For(0, image.Length, i => newImage[i] = Convert.ToByte(i % 4 == 3 ? image[i] * 255 : (image[i] <= 0.00303993 ? image[i] * 3295.418601 : 269.025 * Math.Pow(image[i], 1 / 2.4) - 14.025)));
			} else {
				Parallel.For(0, image.Length, i => newImage[i] = Convert.ToByte(image[i] <= 0.00303993 ? image[i] * 3295.418601 : 269.025 * Math.Pow(image[i], 1 / 2.4) - 14.025));
			}
			return newImage;
		}

		public static float[] LoadRawImage(string path, out int width, out int height, ref PixelFormat pixelFormat) {
			return GammaDecode(LoadImage(path, out width, out height, ref pixelFormat), Image.GetPixelFormatSize(pixelFormat) == 32);
		}

		public static float[] LoadRawImage(string path, out int width, out int height, PixelFormat pixelFormat) {
			return GammaDecode(LoadImage(path, out width, out height, ref pixelFormat), Image.GetPixelFormatSize(pixelFormat) == 32);
		}

		public static void SaveRawImage(float[] image, string path, int width, int height, PixelFormat pixelFormat) {
			SaveImage(GammaEncode(image, Image.GetPixelFormatSize(pixelFormat) == 32), path, width, height, pixelFormat);
		}
	}
}
