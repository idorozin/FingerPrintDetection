/*using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Configuration;
using System.Drawing.Imaging;
using System.IO;
using Emgu;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;

namespace FingerPrintDetection
{
    internal class Detection
    {
        public static void Main(string[] args)
        {
           //Load the image from file and resize it for display
           Image<Bgr, Byte> img =
              new Image<Bgr, byte>(
                    "C:/Users/IDO/RiderProjects/FingerPrintDetection/FingerPrintDetection/g.jpg")
                 .Resize(400, 400, Emgu.CV.CvEnum.Inter.Linear, true);

           //Convert the image to grayscale and filter out the noise
           UMat uimage = new UMat();
           CvInvoke.CvtColor(img, uimage, ColorConversion.Bgr2Gray);

           //use image pyr to remove noise
           UMat pyrDown = new UMat();
           CvInvoke.PyrDown(uimage, pyrDown);
           CvInvoke.PyrUp(pyrDown, uimage);

           //Image<Gray, Byte> gray = img.Convert<Gray, Byte>().PyrDown().PyrUp();

           #region circle detection

           double cannyThreshold = 160.0;
           double circleAccumulatorThreshold = 100;
           CircleF[] circles = CvInvoke.HoughCircles(uimage, HoughType.Gradient, 2.0, 20.0, cannyThreshold, circleAccumulatorThreshold, 5);
           #endregion


           //circleImageBox.Image = circleImage;


           #region Canny and edge detection

           double cannyThresholdLinking = 110.0;
           UMat cannyEdges = new UMat();
           CvInvoke.Canny(uimage, cannyEdges, cannyThreshold, cannyThresholdLinking);

           LineSegment2D[] lines = CvInvoke.HoughLinesP(
              cannyEdges,
              1, //Distance resolution in pixel-related units
              Math.PI / 45.0, //Angle resolution measured in radians.
              20, //threshold
              10, //min Line width
              10); //gap between lines

           #endregion
           
                      List<Triangle2DF> triangleList = new List<Triangle2DF>();

            #region Find triangles and rectangles
            List<RotatedRect> boxList = new List<RotatedRect>(); //a box is a rotated rectangle

            using (VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint())
            {
               CvInvoke.FindContours(cannyEdges, contours, null, RetrType.List, ChainApproxMethod.ChainApproxSimple );
               int count = contours.Size;
               for (int i = 0; i < count; i++)
               {
                  using (VectorOfPoint contour = contours[i])
                  using (VectorOfPoint approxContour = new VectorOfPoint())
                  {
                     Console.WriteLine(approxContour.Size + "Size");

                     CvInvoke.ApproxPolyDP(contour, approxContour, CvInvoke.ArcLength(contour, true) * 0.05, true);
                     if (CvInvoke.ContourArea(approxContour, false) > 250) //only consider contours with area greater than 250
                     {
                        if (approxContour.Size == 3) //The contour has 3 vertices, it is a triangle
                        {
                           Point[] pts = approxContour.ToArray();
                           triangleList.Add(new Triangle2DF(
                              pts[0],
                              pts[1],
                              pts[2]
                              ));
                        } else if (approxContour.Size == 4) //The contour has 4 vertices.
                        {
                           bool isRectangle = true;
                           if (isRectangle) boxList.Add(CvInvoke.MinAreaRect(approxContour));
                        }
                     }
                  }
               }
            }

           #endregion


           List<LineSegment2D> lines_ = new List<LineSegment2D>(); 
           double avgX=0;
           double avgY=0;
           double c=0;

           foreach (var line in lines)
           {
              float x1 = line.P1.X;
              float y1 = line.P1.Y;
   
              bool delete = false;
              foreach (var line_ in lines)
              {
                 float x2 = line_.P1.X;
                 float y2 = line_.P1.Y;

                 if (Distance(x1, x2, y1, y2) <= 4 && Distance(x1, x2, y1, y2) > 0)
                 {
                    Console.WriteLine(Distance(x1, x2, y1, y2));
                    delete = false;
                 }
              }

              if (!delete)
              {
                 Console.WriteLine((line.P1.X + line.P2.X) /2f);
                 Console.WriteLine((line.P1.Y + line.P2.Y) /2f);
                 avgX += (line.P1.X + line.P2.X) /2f;
                 avgY += (line.P1.Y + line.P2.X) /2f;
                 c++;
                 lines_.Add(line);
              }
           }

           avgX = avgX/c+1;
           avgY = avgY/c+1;
           for(int i = lines_.Count-1; i>=0; i--)
           {
              LineSegment2D line = lines_[i];
              double dist = Distance((line.P1.X + line.P2.X) /2f, (float)avgX, (line.P1.Y + line.P2.Y) /2f, (float)avgY);
              if (dist >= 100)
                 lines_.RemoveAt(i);
           }



           #region draw triangles and rectangles

           Image<Bgr, Byte> triangleRectangleImage = img.Copy();
           Image<Gray, Byte> g = triangleRectangleImage.Convert<Gray,byte>();
           Image<Gray, Byte> b = new Image<Gray, byte>(triangleRectangleImage.Width,triangleRectangleImage.Height,new Gray(0));
           float maxX=0;
           float maxY=0;
           float minX=1000;
           float minY=1000;
           LineSegment2D newLine_;
           double cannyThresholdLinking_ = 100.0;
           UMat cannyEdges_ = new UMat();
           CvInvoke.Canny(uimage, cannyEdges_, 130, cannyThresholdLinking_);

           LineSegment2D[] lines__ = CvInvoke.HoughLinesP(
              cannyEdges,
              1, //Distance resolution in pixel-related units
              Math.PI / 45.0, //Angle resolution measured in radians.
              20, //threshold
              10, //min Line width
              10); //gap between lines
           foreach (var box in lines__)
           {
              triangleRectangleImage.Draw(box, new Bgr(Color.Brown), 2);

           }
           foreach (var line in lines_)
           {
              if (line.P1.X > maxX)
                 maxX = line.P1.X; 
              if (line.P2.X > maxX)
                 maxX = line.P2.X; 
              if (line.P1.X < minX)
                 minX = line.P1.X; 
              if (line.P2.X < minX)
                 minX = line.P2.X;         
              if (line.P1.Y > maxY)
                 maxY = line.P1.Y; 
              if (line.P2.Y > maxY)
                 maxY = line.P2.Y; 
              if (line.P1.Y < minY)
                 minY = line.P1.Y; 
              if (line.P2.Y < minY)
                 minY = line.P2.Y;
           }
           PointF p1 = new PointF(minX,minY);
           PointF p2 = new PointF(maxX,maxY);
           LineSegment2D newLine = new LineSegment2D(new Point((int)minX,(int)minY),new Point((int)maxX,(int)maxY));
           PointF p = new PointF((newLine.P1.X+newLine.P2.X)/2f,((newLine.P1.Y+newLine.P2.Y))/2f);
           int height = (int)(maxX - minX + 20);
           int width = (int)(maxY - minY + 20);
           Console.WriteLine(p1 + " " + p2);
           RotatedRect rect = new RotatedRect(p,new SizeF(height,width), 0f);
           foreach (CircleF circle in circles)
           {
              triangleRectangleImage.Draw(circle, new Bgr(Color.Brown), 2);
              Console.WriteLine("circle");
           }


           triangleRectangleImage.Draw(rect, new Bgr(Color.DarkOrange), 2);
          
           //CvInvoke.Threshold(g, b, 500, 255, ThresholdType.Otsu);
           
           triangleRectangleImage.ToBitmap()
              .Save("C:/Users/IDO/RiderProjects/FingerPrintDetection/FingerPrintDetection/f.png");

           #endregion
        }

       private static double Distance(float x1, float x2, float y1, float y2)
       {
          return Math.Sqrt(Math.Pow(x1 - x2,2) + Math.Pow(y1 - y2,2));
       }
    }
}*/