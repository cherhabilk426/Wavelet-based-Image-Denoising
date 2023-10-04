using System;

namespace Tools {
	public class Similarity {
		public static long SE(byte[] a, byte[] b) {
			long total = 0;
			for (int i = 0; i < a.Length; i++) {
				int diff = a[i] - b[i];
				total += diff * diff;
			}
			return total;
		}

		public static long AE(byte[] a, byte[] b) {
			long total = 0;
			for (int i = 0; i < a.Length; i++) {
				total += Math.Abs(a[i] - b[i]);
			}
			return total;
		}

		public static double MSE(byte[] a, byte[] b) {
			return (double) SE(a, b) / a.Length;
		}

		public static double MAE(byte[] a, byte[] b) {
			return (double) AE(a, b) / a.Length;
		}

		public static double RMSE(byte[] a, byte[] b) {
			return Math.Sqrt(MSE(a, b));
		}

		public static double PSNR(byte[] a, byte[] b) {
			return 10 * Math.Log10(65025d / MSE(a, b));
		}

		public static double SSIM(byte[] a, byte[] b, int width, int height, int windowSize = 8) {
			int stride = width * 3;
			double ssim = 0;
			//Iterate over all windows of an image
			for (int y0 = 0; y0 < height; y0 += windowSize) {
				int y1 = Math.Min(y0 + windowSize, height);
				for (int x0 = 0; x0 < width; x0 += windowSize) {
					int x1 = Math.Min(x0 + windowSize, width);
					//Find averages, variances, and covariance
					double aAvg = 0;
					double bAvg = 0;
					double aVar = 0;
					double bVar = 0;
					double cov = 0;
					for (int y = y0; y < y1; y++) {
						int yIndex = y * stride;
						for (int x = x0; x < x1; x++) {
							int index = yIndex + x * 3;
							double aLum = 0.299 * a[index + 2] + 0.587 * a[index + 1] + 0.114 * a[index];
							double bLum = 0.299 * b[index + 2] + 0.587 * b[index + 1] + 0.114 * b[index];
							aAvg += aLum;
							bAvg += bLum;
							aVar += aLum * aLum;
							bVar += bLum * bLum;
							cov += aLum * bLum;
						}
					}
					int windowLength = (y1 - y0) * (x1 - x0);
					aAvg /= windowLength;
					bAvg /= windowLength;
					double aAvgSq = aAvg * aAvg;
					double bAvgSq = bAvg * bAvg;
					double abAvg = aAvg * bAvg;
					aVar = aVar / windowLength - aAvgSq;
					bVar = bVar / windowLength - bAvgSq;
					cov = cov / windowLength - abAvg;

					ssim += (2 * abAvg + 6.5025) * (2 * cov + 58.5225) / ((aAvgSq + bAvgSq + 6.5025) * (aVar + bVar + 58.5225));
				}
			}
			return ssim / ((height / windowSize + (height % windowSize == 0 ? 0 : 1)) * (width / windowSize + (width % windowSize == 0 ? 0 : 1)));
		}
	}
}
