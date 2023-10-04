## Wavelet-based image denoising

### Usage instructions
All programs work with the most common image types such as png, jpeg, and bmp.  
With the exception of the comparer, the programs can also be used by dragging-and-dropping an image file onto the executable.

#### Noise addition
    Noiser.exe "path/to/input image.png"
Asks for noise amount, outputs images corrupted with some different types of noise.

#### Noise removal
    Denoiser.exe "path/to/noisy image.png"
Asks for various denoising parameters, outputs images denoised with some different types of wavelets and thresholds. Optionally also outputs compression estimates in average bits/byte, for each color channel separately, and for the image as a whole.

#### Median and mean filtering
    Meds.exe "path/to/noisy image.png"
Outputs images denoised using median and mean filtering.

#### Comparison
    Comparer.exe "path/to/reference image.png" "path/to/other image.png"
Outputs PSNR and SSIM values of comparing the two given images.
