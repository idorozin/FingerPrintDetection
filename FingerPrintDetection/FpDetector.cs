using System;
using System.Collections.Generic;
using System.Drawing;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;

public class DrawMatches
{
   public static void Main(string[] args)
   {
      //example
      //CropFingerprints("/N1/IMG-20191007-WA0048.jpg", root + "/input/");
   }

   private static void CropFingerprints(string inputImage, string outputfolder)
   {
      Image<Bgr, byte> originalImg = new Image<Bgr, byte>(inputImage).Resize(4000, 4000, Inter.Linear, true);
      Image<Bgr, byte> resizedImg = new Image<Bgr, byte>(inputImage).Resize(400, 400, Inter.Linear, true);
      Image<Bgr, byte> processedImg = resizedImg.Copy();

      var uimage = MorphologicalOperations(ref processedImg);

      var regionsOfIntrest = DetectRegionsOfIntrest(uimage, 0.3, 10000);
      CropAndSave(regionsOfIntrest, originalImg, outputfolder);
   }

   private static List<Rectangle> DetectRegionsOfIntrest(UMat uimage, double maxRatio = 0,
      double maxSize = Double.MaxValue)
   {
      double cannyThresholdLinking = 110.0;
      UMat cannyEdges = new UMat();
      double cannyThreshold = 170.0;
      CvInvoke.Canny(uimage, cannyEdges, cannyThreshold, cannyThresholdLinking);

      List<Rectangle> boxList = new List<Rectangle>();

      using (VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint())
      {
         CvInvoke.FindContours(cannyEdges, contours, null, RetrType.List, ChainApproxMethod.ChainApproxSimple);
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
                     double size = r.Height * r.Width;

                     if (ratio > maxRatio && size < maxSize)
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

   private static UMat MorphologicalOperations(ref Image<Bgr, byte> source2)
   {
      Mat kernal = CvInvoke.GetStructuringElement(ElementShape.Rectangle, new Size(5, 5), new Point(0, 0));
      source2 = source2.MorphologyEx(MorphOp.Open, kernal, new Point(-1, -1), 2, BorderType.Default,
         new MCvScalar(1.0));
      source2 = source2.MorphologyEx(MorphOp.Gradient, kernal, new Point(-1, -1), 1, BorderType.Default,
         new MCvScalar(1.0));
      CvInvoke.Threshold(source2, source2, 25, 255, ThresholdType.Binary);
      UMat uimage = new UMat();
      CvInvoke.CvtColor(source2, uimage, ColorConversion.Bgr2Gray);
      return uimage;
   }

   private static void CropAndSave(List<Rectangle> boxList, Image<Bgr, byte> original, string outputFolder,
      string fileFormat = ".bmp")
   {
      int i8 = 0;
      foreach (Rectangle rect in boxList)
      {
         i8++;
         //int multiplier = original.Width / 400;
         Rectangle rect_ = new Rectangle(rect.Location.X * 10, rect.Location.Y * 10, rect.Size.Width * 10,
            rect.Size.Height * 10);
         //crop
         original.ROI = rect_;
         Image<Bgr, byte> img = original.Copy();
         original.ROI = Rectangle.Empty;
         //process
         var img_ = ImageProcessing(img);
         img_.ToBitmap().Save(outputFolder + i8 + fileFormat);
      }
   }

   private static Image<Gray, byte> ImageProcessing(Image<Bgr, byte> img)
   {
      //processing : Resize , AdaptiveThreshold - define lines , Erode - make lines thicker
      Mat kernal_ = CvInvoke.GetStructuringElement(ElementShape.Rectangle, new Size(2, 2), new Point(0, 0));
      Image<Gray, byte> img_ = img.Convert<Gray, byte>().Resize(500, 500, Inter.Linear, true);
      CvInvoke.AdaptiveThreshold(img_, img_, 255, AdaptiveThresholdType.MeanC, ThresholdType.Binary, 11, 2);
      img_ = img_.MorphologyEx(MorphOp.Erode, kernal_, new Point(-1, -1), 1, BorderType.Default, new MCvScalar(1));
      return img_;
   }
}

