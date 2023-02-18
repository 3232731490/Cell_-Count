using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenCvSharp;
using System.IO;
using System.Xml;
using System.Xml.Linq;

namespace Cell__Count
{
    public class Algorithm
    {
        /// <summary>
        /// 创建、保存XML文件
        /// </summary>
        /// <param name="filename">图像文件名</param>
        /// <param name="info">细胞信息</param>
        /// <param name="n">细胞个数</param>
        private int sava_XML(string filename,Mat info,int n,int winsize)
        {
            XmlDocument doc = new XmlDocument();    // 获取操作XML的doc元素
            XmlDeclaration xmldecl = doc.CreateXmlDeclaration("1.0", "utf-8", null);    // 设置xml文件头部信息
            doc.AppendChild(xmldecl);
            // 创建根元素
            XmlElement root = doc.CreateElement("CELLS");
            // 添加根元素到 XmlDocument 对象
            doc.AppendChild(root);
            int dropNum = 1;
            double rating = 0.0001;    // 判别假阳性阈值
            int count = 0;
            for(int i= 0 ; i< n; i++)
            {
                int area = info.At<int>(i, (int)ConnectedComponentsTypes.Area);
                if ((double)area / winsize < rating){ dropNum++; continue; }    // 去除假阳性
                int x = (int)info.At<int>(i, 0);   //左上角x坐标
                int y = (int)info.At<int>(i, 1);   //左上角y坐标
                int width = (int)info.At<int>(i, 2);   //细胞宽
                int height = (int)info.At<int>(i, 3);  // 细胞高
                // 创建子节点
                XmlElement CELL = doc.CreateElement("CELL");
                // 创建子节点中的元素
                CELL.SetAttribute("ID", count.ToString());
                CELL.AppendChild(doc.CreateElement("POSITIONX")).InnerText = x.ToString();
                CELL.AppendChild(doc.CreateElement("POSITIONY")).InnerText = y.ToString();
                CELL.AppendChild(doc.CreateElement("WIDTH")).InnerText = width.ToString();
                CELL.AppendChild(doc.CreateElement("HEIGHT")).InnerText = height.ToString();
                // 将子节点添加至根节点中
                root.AppendChild(CELL);
                count++;
            }
            root.SetAttribute("COUNT", (n - dropNum).ToString());
            string directoryPath = Path.GetDirectoryName(filename); // 获取文件目录
            string fileName = Path.GetFileNameWithoutExtension(filename);   // 获取文件名(不带后缀)
            string xml_path = directoryPath + "\\" + MainWindow.XML_Path + "\\" + fileName + ".xml";
            doc.Save(xml_path); // 将细胞信息保存至磁盘
            return dropNum;
        }

        /// <summary>
        /// 细胞计数具体算法
        /// </summary>
        /// <param name="filename">细胞图像路径</param>
        /// <returns></returns>
        public int _solve(string filename) {
            Mat img = new Mat(filename, ImreadModes.Color);
            Mat src = new Mat();
            Cv2.CvtColor(img, src, ColorConversionCodes.BGR2GRAY);
            Cv2.GaussianBlur(src, src, new OpenCvSharp.Size(7, 7), 0, 0);
            Mat thresh = new Mat();
            Cv2.Threshold(src, thresh, 0, 255, ThresholdTypes.Otsu);       
            Mat dilate = new Mat(); Mat element = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(3, 3)); Cv2.Erode(thresh, dilate, element);
            Mat labels = new Mat();
            Mat stats = new Mat();
            Mat centroids = new Mat();
            int nLabels = Cv2.ConnectedComponentsWithStats(dilate, labels, stats, centroids, PixelConnectivity.Connectivity8);
            int cellCount = nLabels;    // 减去图像本身这个连通域
            int winsize = img.Width * img.Height;
            int dropnum = sava_XML(filename, stats, nLabels, winsize);
            return cellCount -  dropnum;
        }

        private void Water_Save_XML(string filename,Mat info , int n)
        {
            XmlDocument doc = new XmlDocument();    // 获取操作XML的doc元素
            XmlDeclaration xmldecl = doc.CreateXmlDeclaration("1.0", "utf-8", null);    // 设置xml文件头部信息
            doc.AppendChild(xmldecl);
            // 创建根元素
            XmlElement root = doc.CreateElement("CELLS");
            // 添加根元素到 XmlDocument 对象
            doc.AppendChild(root);
            for (int i = 0; i < n; i++)
            {
                int x = (int)info.At<int>(i, 0);   //左上角x坐标
                int y = (int)info.At<int>(i, 1);   //左上角y坐标
                int width = (int)info.At<int>(i, 2);   //细胞宽
                int height = (int)info.At<int>(i, 3);  // 细胞高
                // 创建子节点
                XmlElement CELL = doc.CreateElement("CELL");
                // 创建子节点中的元素
                CELL.SetAttribute("ID", i.ToString());
                CELL.AppendChild(doc.CreateElement("POSITIONX")).InnerText = x.ToString();
                CELL.AppendChild(doc.CreateElement("POSITIONY")).InnerText = y.ToString();
                CELL.AppendChild(doc.CreateElement("WIDTH")).InnerText = width.ToString();
                CELL.AppendChild(doc.CreateElement("HEIGHT")).InnerText = height.ToString();
                // 将子节点添加至根节点中
                root.AppendChild(CELL);
            }
            root.SetAttribute("COUNT", (n - 1).ToString());
            string directoryPath = Path.GetDirectoryName(filename); // 获取文件目录
            string fileName = Path.GetFileNameWithoutExtension(filename);   // 获取文件名(不带后缀)
            string xml_path = directoryPath + "\\" + MainWindow.XML_Path + "\\" + fileName + ".xml";
            doc.Save(xml_path); // 将细胞信息保存至磁盘
        }
        public int Water_Algorithm_solve(string filename)
        {
            Mat image = new Mat(filename, ImreadModes.Color);
            // 预处理
            // 边缘保留滤波EPF  去噪
            Mat blur = new Mat();
            Cv2.PyrMeanShiftFiltering(image, blur, 21, 55);
            // 转成灰度图像
            Mat gray = new Mat();
            Cv2.CvtColor(blur, gray, ColorConversionCodes.BGR2GRAY);
            Mat binary = new Mat();
            // 得到二值图像区间阈值
            Cv2.Threshold(gray, binary, 0, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);

            // 距离变换
            Mat dist = new Mat();
            Cv2.DistanceTransform(binary, dist, DistanceTypes.L2, DistanceTransformMasks.Mask3);
            Cv2.Normalize(dist, dist, 0, 1.0, NormTypes.MinMax);
            double  maxVal;
            Cv2.MinMaxLoc(dist, out _, out maxVal, out _, out _);
            Mat surface = new Mat();
            Cv2.Threshold(dist, surface, 0.2 * maxVal, 255, ThresholdTypes.Binary);
            Mat sure_fg = new Mat();
            surface.ConvertTo(sure_fg, MatType.CV_8UC1);// 转成8位整型

            Mat labels = new Mat();
            // Marker labelling
            Cv2.ConnectedComponents(sure_fg, labels, PixelConnectivity.Connectivity8, MatType.CV_32S);
            Cv2.MinMaxLoc(labels, out _, out maxVal, out _, out _);
            int ret = (int)maxVal;
            labels += 1; // 整个图+1，使背景不是0而是1值

            // 未知区域标记(不能确定是前景还是背景)
            Mat kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(3, 3));
            Mat binary_dilate = new Mat();
            Cv2.Erode(binary, binary_dilate, kernel, iterations: 1);

            Mat unknown = binary_dilate - sure_fg;

            for (int i = 0; i < unknown.Rows; i++)
            {
                for (int j = 0; j < unknown.Cols; j++)
                {
                    // 未知区域标记为0
                    if (unknown.At<int>(i, j) == 255)
                        labels.At<int>(i, j) = 0;
                }
            }
            // 区域标记结果
            Mat markers_show = labels.ConvertScaleAbs(100);

            // 分水岭算法分割
            Cv2.Watershed(image, labels);
            Cv2.MinMaxLoc(labels, out _, out double max_val, out _, out _);
            Mat markers_8u = labels.ConvertScaleAbs();

            Mat info = new Mat((int)max_val, 4, MatType.CV_32SC1); // 创建info对象，存储细胞信息
            Mat thres1 = new Mat();
            Mat thres2 = new Mat();
            // 第一行存储图像信息
            info.At<int>(0, 0) = 0; 
            info.At<int>(0, 1) = 0; 
            info.At<int>(0, 2) = image.Width; 
            info.At<int>(0, 3) = image.Height; 
            for (int i = 2; i <= (int)maxVal; i++)
            {
                Cv2.Threshold(markers_8u, thres1, i - 1, 255, ThresholdTypes.Binary);
                Cv2.Threshold(markers_8u, thres2, i, 255, ThresholdTypes.Binary);
                var mask = thres1 - thres2;
                var contours = Cv2.FindContoursAsArray(mask, RetrievalModes.External, ContourApproximationModes.ApproxNone);
                if (contours.Length == 0) continue;
                var boundingRect = Cv2.BoundingRect(contours[0]);
                // 输出外接矩形左上角坐标和宽高信息
                info.At<int>(i - 1, 0) = boundingRect.X; // 外接矩形左上角x
                info.At<int>(i - 1, 1) = boundingRect.Y; // 外接矩形左上角y
                info.At<int>(i - 1, 2) = boundingRect.Width; // 外接矩形宽
                info.At<int>(i - 1, 3) = boundingRect.Height; // 外接矩形高
            }
            int winsize = image.Width * image.Height;
            Water_Save_XML(filename, info, (int)max_val);
            return (int)max_val - 1;
        }
    }
}
