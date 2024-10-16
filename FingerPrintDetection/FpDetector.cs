
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;

public class FpDetector
{
   public static void Main(string[] args)
   {
      if (args.Length < 2)
      {
         Console.WriteLine("Usage: FpDetector <input_directory> <output_directory>");
         return;
      }

      string inputDirectory = args[0];
      string outputDirectory = args[1];

      if (!Directory.Exists(inputDirectory))
      {
         Console.WriteLine("Error: Input directory does not exist.");
         return;
      }

      if (!Directory.Exists(outputDirectory))
      {
         Directory.CreateDirectory(outputDirectory);
      }

      string[] files = Directory.GetFiles(inputDirectory, "*", SearchOption.AllDirectories);
      int processedFilesCount = 0;

      foreach (var file in files)
      {
         processedFilesCount++;
         CropFingerprints(file, Path.Combine(outputDirectory, "output_" + processedFilesCount.ToString()));
      }

      Console.WriteLine($"Processed {processedFilesCount} files.");
   }

   private static void CropFingerprints(string inputImage, string outputfolder)
   {
      Image<Bgr, byte> originalImg = new Image<Bgr, byte>(inputImage).Resize(4000, 4000, Inter.Linear, true);
      Image<Bgr, byte> resizedImg = new Image<Bgr, byte>(inputImage).Resize(400, 400, Inter.Linear, true);
      Image<Bgr, byte> processedImg = resizedImg.Copy();

      var uimage = MorphologicalOperations(processedImg);

      var regionsOfIntrest = DetectRegionsOfIntrest(uimage, 0.35, 10000);

      if (regionsOfIntrest.Count == 0)
      {
         var copy = resizedImg.Copy();
         uimage = MorphologicalOperations(copy,false);
         regionsOfIntrest = DetectRegionsOfIntrest(uimage, 0.35, 10000);
      }    
      if (regionsOfIntrest.Count == 0)
      {
         var copy = resizedImg.Copy();
         uimage = MorphologicalOperations(copy,false,true);
         regionsOfIntrest = DetectRegionsOfIntrest(uimage, 0.35, 10000,true);
      }  

      if(CropAndSave(regionsOfIntrest, originalImg, outputfolder,false)==0)
         if(CropAndSave(regionsOfIntrest, originalImg, outputfolder,true)==0)
            uimage.Bitmap.Save(outputfolder+"hhh.jpg");
   }

   private static List<Rectangle> DetectRegionsOfIntrest(UMat uimage, double maxRatio = 0,
      double maxSize = Double.MaxValue , bool hardD = false)
   {
      double cannyThresholdLinking = 110.0;
      UMat cannyEdges = new UMat();
      double cannyThreshold = 170.0;
      CvInvoke.Canny(uimage, cannyEdges, cannyThreshold, cannyThresholdLinking);

      List<Rectangle> boxList = new List<Rectangle>();

      using (VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint())
      {
         CvInvoke.FindContours(cannyEdges, contours, null, RetrType.List, ChainApproxMethod.LinkRuns);
         int count = contours.Size;
         for (int i = 0; i < count; i++)
         {
            using (VectorOfPoint contour = contours[i])
            using (VectorOfPoint approxContour = new VectorOfPoint())
            {
               CvInvoke.ApproxPolyDP(contour, approxContour, CvInvoke.ArcLength(contour, true) * 0.05, true);
               if (CvInvoke.ContourArea(approxContour, false) > 250) //only consider contours with area greater than 250
               {
                  if (approxContour.Size > 1)
                  {
                     bool isRectangle = true;
                     Rectangle r = CvInvoke.BoundingRectangle(contours[i]);
                     double ratio = Math.Min((double) r.Height, r.Width) / Math.Max((double) r.Height, r.Width);
                     double hToW = (double)r.Height / r.Width;
                     double size = r.Height * r.Width;

                     if (hardD ||( ratio > maxRatio && size < maxSize && hToW > 0.85))
                     {
                        foreach (var rect in boxList)
                        {
                           if (rect.Location == r.Location)
                           {
                              isRectangle = false;
                              break;
                           }
                        }

                        if (isRectangle)
                        {
                           boxList.Add(CvInvoke.BoundingRectangle(contours[i]));
                        }
                     }
                  }
               }
            }
         }
      }

      return boxList;
   }

   private static UMat MorphologicalOperations(Image<Bgr, byte> copy, bool open = true , bool hardD = false)
   {
      Mat kernel =  CvInvoke.GetStructuringElement(Emgu.CV.CvEnum.ElementShape.Rectangle, new Size(3, 3), new Point(-1, -1));
      CvInvoke.Erode(copy, copy, kernel, new Point(-1, -1), 1, Emgu.CV.CvEnum.BorderType.Default, new MCvScalar(0));
      CvInvoke.Dilate(copy, copy, kernel, new Point(-1, -1), 8, Emgu.CV.CvEnum.BorderType.Default, new MCvScalar(0));
      CvInvoke.Erode(copy, copy, kernel, new Point(-1, -1), 7, Emgu.CV.CvEnum.BorderType.Default, new MCvScalar(0));
      Mat kernal = CvInvoke.GetStructuringElement(ElementShape.Rectangle, new Size(5, 5), new Point(0, 0));
      copy = copy.MorphologyEx(MorphOp.Gradient, kernal, new Point(-1, -1), 1, BorderType.Default,
         new MCvScalar(1.0));  
      CvInvoke.Threshold(copy, copy, 15, 255, ThresholdType.Binary);



      UMat uimage = new UMat();
      CvInvoke.CvtColor(copy, uimage, ColorConversion.Bgr2Gray);
     Random rnd = new Random();
      var r = rnd.Next();
      return uimage;
   }

   private static int CropAndSave(List<Rectangle> boxList, Image<Bgr, byte> original, string outputFolder,
      bool hardD = false,
      string fileFormat = ".bmp")
   {
      List<Image<Gray,byte>> imgs = new List<Image<Gray, byte>>();
      int counter =0;
      int len = boxList.Count;
      int i8 = 0;
      bool c = false;
      int realCount = 0;
      if(boxList.Count > 1)
      foreach (var rect in boxList)
      {
         foreach (Rectangle _rect in boxList)
         {
            if (_rect != rect && _rect.Contains(rect.Location) && _rect.Height * _rect.Width > rect.Height * rect.Width)
            {
               {
                  c = true;
                  break;
               }
            }

            if (c)
               continue;
            realCount++;
         }
      }

      c = false;

      foreach (Rectangle rect in boxList)
      {
            foreach (Rectangle _rect in boxList)
            {
               if (_rect != rect && _rect.Contains(rect.Location) &&
                   _rect.Height * _rect.Width > rect.Height * rect.Width)
               {
                     c = true;
                     break;
               }
            }


         if (c)
         {
            c = false;
            continue;
         }

            
            i8++;
            //int multiplier = original.Width / 400;
            Rectangle rect_ = new Rectangle(rect.Location.X * 10, rect.Location.Y * 10, rect.Size.Width * 10,
               rect.Size.Height * 10);
            //crop
            original.ROI = rect_;
            Image<Bgr, byte> img = original.Copy();
            original.ROI = Rectangle.Empty;
            //process
             bool isG = true;
            if (realCount > 1)
            {
               var copy = img.Copy().Convert<Gray,byte>().Resize(400,400,Inter.Linear,true);
               var copy_ = copy.Copy();
               CvInvoke.Threshold(copy, copy, 80, 255, ThresholdType.Binary);
               CvInvoke.Threshold(copy_, copy_, 30, 255, ThresholdType.Binary);
               isG = IsGrayScale(copy.Bitmap) > 90 && IsGrayScale(copy_.Bitmap) > 98;
            }

            if (isG || hardD)
            {
               var img_ = ImageProcessing(img);
               double v = IsGrayScale(img_.Copy().Bitmap);
               if (hardD || v < 70)
               {
                  counter++;
                  imgs.Add(img_);
               }
            }
      }

      Image<Gray, byte> selected = null;

      if (imgs.Count > 1)
      {
     selected = imgs[0];
        double val = IsGrayScale(selected.Bitmap);
         foreach (var img in imgs)
         {
            double current = IsGrayScale(img.Bitmap);
            if (current < val)
            {
               selected = img;
               val = current;
            }
         }

         if (val < 40)
            selected = null;
      }
      else
      {
         if(imgs.Count==1)
         selected = imgs[0];
      }

      if (selected != null)
      {
         selected.ToBitmap().Save(outputFolder + "_" + IsGrayScale(selected.ToBitmap()) + fileFormat);
      }

      return counter;
   }

   private static Image<Gray, byte> ImageProcessing(Image<Bgr, byte> img)
   {

      //processing : Resize , AdaptiveThreshold - define lines , Erode - make lines thicker
      Mat kernal_ = CvInvoke.GetStructuringElement(ElementShape.Rectangle, new Size(2, 2), new Point(0, 0));
      Image<Gray, byte> img_ = img.Convert<Gray, byte>().Resize(500, 500, Inter.Linear, true);
      Image<Gray, byte> mask = img_.Convert<Gray, byte>();
      Mat kernel =  CvInvoke.GetStructuringElement(Emgu.CV.CvEnum.ElementShape.Rectangle, new Size(3, 3), new Point(-1, -1));
      CvInvoke.Dilate(mask, mask, kernel, new Point(-1, -1), 1, Emgu.CV.CvEnum.BorderType.Default, new MCvScalar(0));
      CvInvoke.Erode(mask, mask, kernel, new Point(-1, -1), 8, Emgu.CV.CvEnum.BorderType.Default, new MCvScalar(0));
      CvInvoke.Dilate(mask, mask, kernel, new Point(-1, -1), 7, Emgu.CV.CvEnum.BorderType.Default, new MCvScalar(0));
      CvInvoke.Threshold(mask, mask, 162, 255, ThresholdType.BinaryInv);
      mask = mask.Not();
     CvInvoke.Blur(mask, mask, new Size(5,5), new Point(-1, -1));
      CvInvoke.AdaptiveThreshold(img_, img_, 255, AdaptiveThresholdType.MeanC, ThresholdType.Binary, 11, 2);
      img_ = img_.MorphologyEx(MorphOp.Erode, kernal_, new Point(-1, -1), 1, BorderType.Default, new MCvScalar(1)).Not();      
      return img_.Copy(mask.Not()).Not();
   }
   
   private static unsafe double IsGrayScale(Image image)
   {
      using (var bmp = new Bitmap(image.Width, image.Height, PixelFormat.Format32bppArgb))
      {
         using (var g = Graphics.FromImage(bmp))
         {
            g.DrawImage(image, 0, 0);
         }

         var data = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, bmp.PixelFormat);

         var pt = (int*)data.Scan0;
         var res = true;
         int count = 0;
         
         for (var i = 0; i < data.Height * data.Width; i++)
         {
            var color = Color.FromArgb(pt[i]);
            double max = Math.Max(Math.Max(color.R/255.0, color.G/255.0), color.B/255.0);
            double min = Math.Min(Math.Min(color.R/255.0, color.G/255.0), color.B/255.0);
            double luminace = (max+min)/2.0;
            double sat = 0;
            if (luminace < 0.5)
               sat = (max - min) / (max + min);
            else
               sat = (max - min) / (2.0 - max - min);
            if (color.G == 255 &&  color.B == 255 && color.R == 255)
            {
               count++;
            }
         }
         bmp.UnlockBits(data);
         return (double)count/(data.Height * data.Width)*100;
      }
   }
}


