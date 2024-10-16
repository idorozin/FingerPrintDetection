# Fingerprint detection project
This is a old project I made for a friend. It is designed to detect and extract regions of interest (such as fingerprints) from images using the Emgu CV library, which is a .NET wrapper for the popular OpenCV image processing library.

![image](https://github.com/user-attachments/assets/7f99b098-a6f4-4800-8ba6-707546773d69)

## How It works
### Fingerprint Detection
The process uses morphological image processing techniques like **Erosion** and **Dilation** to clean noise and refine the contours of objects in the image:

- **Erosion**: Shrinks bright areas, removing small noise.
- **Dilation**: Expands bright areas, emphasizing significant shapes.

These techniques help highlight the regions of interest, such as fingerprints.

<img src="https://github.com/user-attachments/assets/fc67a2dd-f2b2-4185-82e6-61a25adbfe3f" width="400" alt="Erosion and Dilation effects"/>

Next, we apply **Edge Detection** to locate the boundaries of these regions of interest.

<img src="https://github.com/user-attachments/assets/8feffb50-42f4-46f4-ac2e-ec692ddce656" width="400" alt="Edge detection"/>

Finally, we calculate the **bounding box** of the detected contours, isolating the fingerprint region.

<img src="https://github.com/user-attachments/assets/dadc0dfa-d855-499b-be11-8a9ea933d46e" width="200" alt="Bounding box"/>

### Post-Processing

Once the fingerprint is detected, we crop the image. However, the cropped image might still lack quality, as shown below:

<img src="https://github.com/user-attachments/assets/8bfe6fda-5473-441b-b6d0-94f2a892e4bf" width="120" alt="Before Post-Processing"/>

To enhance the fingerprint's visibility, we apply image processing effects to improve clarity and detail.

<img src="https://github.com/user-attachments/assets/07934ef5-d140-4d64-955f-51713203e157" width="120" alt="After Post-Processing"/>

## Quick Start
to try the project clone the repo and build it with ```dotnet build```

Usage: ```FpDetector <input_directory> <output_directory> (the input directory should cotain images)```
