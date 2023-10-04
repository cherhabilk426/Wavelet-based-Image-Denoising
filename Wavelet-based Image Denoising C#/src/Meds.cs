using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Threading.Tasks;

class Meds {
	static void Main(string[] args) {
		//Get file data
		string inPath = args.Length > 0 ? args[0] : Console.ReadLine();
		int extensionIndex = inPath.LastIndexOf('.');
		if (extensionIndex == -1) {
			extensionIndex = inPath.Length;
		}
		string fileName = inPath.Substring(0, extensionIndex);
		byte[] inImage = Tools.IO.LoadImage(inPath, out int width, out int height, PixelFormat.Format24bppRgb);
		byte[] outMean = new byte[inImage.Length];
		byte[] outMedian = new byte[inImage.Length];

		//Median and mean filter
		Parallel.For(0, height, y => {
			List<byte> values = new List<byte>(9);
			for (int x = 0; x < width; x++) {
				for (int z = 0; z < 3; z++) {
					int index = (y * width + x) * 3 + z;
					int sum = 0;
					values.Clear();

					for (int j = -1; j < 2; j++) {
						int y2 = y + j;
						if (y2 < 0) {
							y2 = 0;
						} else if (y2 >= height) {
							y2 = height - 1;
						}

						for (int i = -1; i < 2; i++) {
							int x2 = x + i;
							if (x2 < 0) {
								x2 = 0;
							} else if (x2 >= width) {
								x2 = width - 1;
							}

							byte value = inImage[(y2 * width + x2) * 3 + z];
							sum += value;
							values.Add(value);
						}
					}

					outMean[index] = (byte) (sum / 9f + 0.5f);
					values.Sort();
					outMedian[index] = values[4];
				}
			}
		});

		//Save results
		Tools.IO.SaveImage(outMean, fileName + " Simple Mean Denoised.png", width, height, PixelFormat.Format24bppRgb);
		Tools.IO.SaveImage(outMedian, fileName + " Simple Median Denoised.png", width, height, PixelFormat.Format24bppRgb);
	}
}
