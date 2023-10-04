using System;
using System.Drawing;
using System.Drawing.Imaging;

class Noiser {
	static void Main(string[] args) {
		//Get file data
		string inPath = args.Length > 0 ? args[0] : Console.ReadLine();
		int extensionIndex = inPath.LastIndexOf('.');
		if (extensionIndex == -1) {
			extensionIndex = inPath.Length;
		}
		string fileName = inPath.Substring(0, extensionIndex);
		PixelFormat pixelFormat = PixelFormat.DontCare;
		byte[] inImage = Tools.IO.LoadImage(inPath, out int width, out int height, ref pixelFormat);
		int bpp = Image.GetPixelFormatSize(pixelFormat) / 8;

		//Ask for noise level
		Console.WriteLine("Enter noise amount [0-100]:");
		double level = double.Parse(args.Length > 1 ? args[1] : Console.ReadLine()) * 0.01;

		//Apply various types of noise and save the images
		byte[] imageCopy = new byte[inImage.Length];

		//Level - Percentage of pixels that are noisy
		//Salt and pepper - separate
		inImage.CopyTo(imageCopy, 0);
		Tools.Noise.SaltAndPepper(imageCopy, level);
		Tools.IO.SaveImage(imageCopy, fileName + " SnP " + level + ".png", width, height, pixelFormat);
		//Salt and pepper - together
		//inImage.CopyTo(imageCopy, 0);
		//Tools.Noise.SaltAndPepper(imageCopy, level, bpp, true);
		//Tools.IO.SaveImage(imageCopy, fileName + " SnP tog.png", width, height, pixelFormat);
		//Impulse - separate
		inImage.CopyTo(imageCopy, 0);
		Tools.Noise.Impulse(imageCopy, level);
		Tools.IO.SaveImage(imageCopy, fileName + " Imp " + level + ".png", width, height, pixelFormat);
		//Impulse - together
		//inImage.CopyTo(imageCopy, 0);
		//Tools.Noise.Impulse(imageCopy, level, bpp, true);
		//Tools.IO.SaveImage(imageCopy, fileName + " Imp tog.png", width, height, pixelFormat);
		//Speckle - separate
		inImage.CopyTo(imageCopy, 0);
		Tools.Noise.Speckle(imageCopy, level);
		Tools.IO.SaveImage(imageCopy, fileName + " Spec " + level + ".png", width, height, pixelFormat);
		//Speckle - together
		//inImage.CopyTo(imageCopy, 0);
		//Tools.Noise.Speckle(imageCopy, level, bpp, true);
		//Tools.IO.SaveImage(imageCopy, fileName + " Spec tog.png", width, height, pixelFormat);

		//Level - How many units in one standard deviation (255 units in the full range)
		//Gaussian - separate
		inImage.CopyTo(imageCopy, 0);
		Tools.Noise.Gaussian(imageCopy, level * 100);
		Tools.IO.SaveImage(imageCopy, fileName + " Gaus " + level + ".png", width, height, pixelFormat);
		//Gaussian - together
		//inImage.CopyTo(imageCopy, 0);
		//Tools.Noise.Gaussian(imageCopy, level * 100, bpp, true);
		//Tools.IO.SaveImage(imageCopy, fileName + " Gaus tog.png", width, height, pixelFormat);

		//Level - Inverse of how many pixels hit the sensor on average on a logarithmic scale
		//0 ~= 516, 50 ~= 8.02, 100 ~= 0.125
		//Poisson - separate
		inImage.CopyTo(imageCopy, 0);
		Tools.Noise.Poisson(imageCopy, Math.Pow(2.3, 7.5 - 10 * level));
		Tools.IO.SaveImage(imageCopy, fileName + " Pois " + level + ".png", width, height, pixelFormat);
		//Poisson - together
		//inImage.CopyTo(imageCopy, 0);
		//Tools.Noise.Poisson(imageCopy, Math.Pow(2.3, 7.5 - 10 * level), bpp, true);
		//Tools.IO.SaveImage(imageCopy, fileName + " Pois tog.png", width, height, pixelFormat);
	}
}
