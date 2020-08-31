using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Drawing.Imaging;

namespace PhotoInfoFix.Services
{
    public class ExifFix
    {
        public static void FixExifDateTime(string photoPath, string photoTrueTime, string photoFalseTime)
        {
            IFormatProvider culture = new CultureInfo("zh-CN", true);
            DateTime timeTrue = DateTime.ParseExact(photoTrueTime, "yyyy:MM:dd HH:mm:ss", culture); //某个照片的实际拍摄时间
            DateTime timeFalse = DateTime.ParseExact(photoFalseTime, "yyyy:MM:dd HH:mm:ss", culture); //该照片EXIF的记录时间
            TimeSpan timeSpan = timeTrue - timeFalse; //计算照片时间差
            FixExifDateTime(photoPath, timeSpan);
        }

        public static void FixExifDateTime(string photoPath, TimeSpan timeSpan)
        {
            var files = Directory.GetFiles(photoPath); //获取所有照片路径
            IFormatProvider culture = new CultureInfo("zh-CN", true);
            foreach (var file in files)
            {
                //替代System.Drawing.Image.FromFile(file);
                System.Drawing.Image image = GetImage(file);

                //获取Exif的DateTimeOriginal属性（36867）
                PropertyItem pi = image.GetPropertyItem(36867); //(int)ExifTags.DateTimeOriginal

                //转为时间文本
                string oldTime = Encoding.ASCII.GetString(pi.Value);
                oldTime = oldTime.Replace("\0", "");

                Debug.WriteLine(file + " " + oldTime);
                //时间文本格式化为DateTime
                DateTime time = DateTime.ParseExact(oldTime, "yyyy:MM:dd HH:mm:ss", culture);

                //由于是接着ExifToolGui没有改完的目录，所以只转换EXIF记录为timeFalse年份的，
                //跨年的话要另外处理，因为我这里不跨年，所以简单点

                //得到正确的时间
                DateTime newTime = time + timeSpan;

                //转换为EXIF存储的时间格式
                string newTimeString = newTime.ToString("yyyy:MM:dd HH:mm:ss");
                pi.Value = Encoding.ASCII.GetBytes(newTimeString + "\0");

                //修改DateTimeOriginal属性和其他的时间属性
                image.SetPropertyItem(pi);
                pi.Id = 306; // (int)ExifTags.DateTime; //306
                image.SetPropertyItem(pi);
                pi.Id = 36868; //(int)ExifTags.DateTimeDigitized; //36868
                image.SetPropertyItem(pi);

                //存回文件
                image.Save(file);

                image.Dispose();
            }
        }

        public static DateTime GetDateTimeOriginal(System.Drawing.Image image)
        {
            PropertyItem pi = image.GetPropertyItem(36867); //(int)ExifTags.DateTimeOriginal
            string oldTime = Encoding.ASCII.GetString(pi.Value);
            oldTime = oldTime.Replace("\0", "");

            //时间文本格式化为DateTime
            IFormatProvider culture = new CultureInfo("zh-CN", true);
            DateTime time = DateTime.ParseExact(oldTime, "yyyy:MM:dd HH:mm:ss", culture);
            return time;
        }

        public static System.Drawing.Image GetImage(string filePath)
        {
            try
            {
	            if (!File.Exists(filePath))
	            {
	                return null;
	            }
	
	            using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
	            {
	                byte[] bytes = new byte[fileStream.Length];
	                fileStream.Read(bytes, 0, bytes.Length);
	
	                MemoryStream memoryStream = new MemoryStream(bytes);
	                if (memoryStream != null)
	                {
	                    return System.Drawing.Image.FromStream(memoryStream);
	                }
	            }
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine(ex);
            }

            return null;
        }
    }
}
