using System;
using System.Drawing.Imaging;

class Comparer {
	static void Main(string[] args) {
		byte[] image1 = Tools.IO.LoadImage(args[0], out int width1, out int height1, PixelFormat.Format24bppRgb);
		byte[] image2 = Tools.IO.LoadImage(args[1], out int width2, out int height2, PixelFormat.Format24bppRgb);
		Console.WriteLine("PSNR: " + Tools.Similarity.PSNR(image1, image2));
		Console.WriteLine("SSIM: " + Tools.Similarity.SSIM(image1, image2, width1, height1));
	}
}
