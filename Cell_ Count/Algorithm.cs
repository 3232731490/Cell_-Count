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
        private int sava_XML(string filename,Mat info,Mat lables,int n,int winsize)
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
            for(int i= 0 ; i<n; i++)
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
                CELL.SetAttribute("ID", i.ToString());
                CELL.AppendChild(doc.CreateElement("POSITIONX")).InnerText = x.ToString();
                CELL.AppendChild(doc.CreateElement("POSITIONY")).InnerText = y.ToString();
                CELL.AppendChild(doc.CreateElement("WIDTH")).InnerText = width.ToString();
                CELL.AppendChild(doc.CreateElement("HEIGHT")).InnerText = height.ToString();
                // 将子节点添加至根节点中
                root.AppendChild(CELL);
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
            int dropnum = sava_XML(filename, stats,labels, nLabels, winsize);
            return cellCount -  dropnum;
        }
    }
}
