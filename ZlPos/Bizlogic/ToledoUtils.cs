﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Xml;
using System.Xml.Linq;

namespace ZlPos.Bizlogic
{
    class ToledoUtils
    {

        //Dll function declaration 
        //Dll 函数声明
        [DllImport("Library\\TOLEDO\\MTScaleAPI.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern bool ExecuteTaskInFile(string szTaskID, string szInputFile, string szOutputFile, bool bSynch);

        [DllImport("Library\\TOLEDO\\MTScaleAPI.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr QueryTask(string szInput);

        [DllImport("Library\\TOLEDO\\MTScaleAPI.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern void Free(IntPtr p);

        public string ip { get; set; }
        public string port { get; set; }
        public string TaskPath { get; set; }
        public string OutputFile { get; set; }

        public string TaskID { get; set; }

        //查询执行结果用
        private string CommandID = "";

        public bool ClearData { get; set; }


        //public ToledoUtils() { }

        ///// <summary>
        ///// 
        ///// </summary>
        ///// <param name="ip"></param>
        ///// <param name="TaskPath"></param>
        ///// <param name="port"></param>
        //public ToledoUtils(string ip, string TaskPath, string port = "3001",string OutputFile = "TaskResult.xml")
        //{
        //    this.ip = ip;
        //    this.TaskPath = TaskPath;
        //    this.port = port;
        //    this.OutputFile = OutputFile;
        //}

        public bool ExecuteTaskInFile(bool bSynch = false)
        {
            if (string.IsNullOrEmpty(TaskID))
            {
                return false;
            }

            return ExecuteTaskInFile(TaskID, TaskPath + "\\Task.xml", TaskPath + "\\TaskResult.xml", bSynch);
        }

        /// <summary>
        /// 查询任务状态来获取结果
        /// </summary>
        /// <returns></returns>
        public bool QueryTask()
        {
            bool result = false;
            string strQueryTaskInput = "<MTTask><TaskID>" + TaskID + "</TaskID><TaskType>98</TaskType></MTTask>";
            while (true)
            {
                //call QueryTask
                //调用查询任务状态方法。
                IntPtr p = QueryTask(strQueryTaskInput);
                string strTmp = Marshal.PtrToStringAnsi(p);
                //free returned string resource.
                //释放动态库返回的字符串资源
                Free(p);

                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(strTmp);

                if (xmlDoc.SelectSingleNode("MTTaskResult") != null)
                {
                    if (xmlDoc.SelectSingleNode("MTTaskResult").SelectSingleNode("TaskStatus") != null)
                    {

                        if (xmlDoc.SelectSingleNode("MTTaskResult").SelectSingleNode("TaskStatus").InnerText.Equals("Complete", StringComparison.OrdinalIgnoreCase))
                        {
                            //task execute complete .
                            //任务执行结束
                            result = true;
                            break;
                        }
                        else if (xmlDoc.SelectSingleNode("MTTaskResult").SelectSingleNode("TaskStatus").InnerText.Equals("Error", StringComparison.OrdinalIgnoreCase))
                        {
                            result = false;
                            break;
                        }
                        else
                        {
                            Thread.Sleep(100);
                            continue;
                        }
                    }
                }
            }

            return result;
        }


        /// <summary>
        /// 生成一个任务 相关文件 （数据需另外生成）
        /// </summary>
        /// <returns></returns>
        public void BuildTask(string guid)
        {

            TaskID = guid;

            XDocument TaskXml = new XDocument();
            TaskXml.Add(GetTaskX(TaskID: guid));
            TaskXml.Save(TaskPath + "\\Task.xml");

            XDocument DeviceListXml = new XDocument();
            DeviceListXml.Add(GetDeviceListX(ip, port));
            DeviceListXml.Save(TaskPath + "\\DeviceList.xml");

            CommandID = Guid.NewGuid().ToString();
            XDocument CommandXml = new XDocument();
            CommandXml.Add(GetCommandX(CommandID: Guid.NewGuid().ToString(), ClearData: ClearData));
            CommandXml.Save(TaskPath + "\\Command.xml");

            //XDocument DataXml = new XDocument();
            //DataXml.Add(new XElement("Data",GetItem()


        }

        public void BuildData()//string PLU, string commodityName, string price, string indate, string tare)
        {
            XDocument DataXml = new XDocument();
            DataXml.Add(new XElement("Data"));//, GetItem(PLU: PLU, commodityName: commodityName, price: price, indate: indate, tare: tare)));
            DataXml.Save(TaskPath + "\\Data.xml");
        }

        public void AddData(string PLU, string commodityName, string price, string indate, string tare, string barcode, string type)
        {
            XDocument dataxml = XDocument.Load(TaskPath + "\\Data.xml");
            dataxml.Element("Data").Add(GetItem(PLU: PLU, commodityName: commodityName, price: price, indate: indate, tare: tare, barcode: barcode, type: type));
            dataxml.Save(TaskPath + "\\Data.xml");
        }





        /// <summary>
        /// 获取DeviceList.xml的内容 //暂时只支持单台 后面考虑添加多台兼容
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        /// <returns></returns>
        public XElement GetDeviceListX(string ip, string port)
        {
            XElement DeviceList = new XElement("Devices",
            new XElement("Scale",
                new XElement("DeviceID", "1"),
                new XElement("ScaleNo", "2"),
                new XElement("ScaleType", "bPlus"),
                new XElement("ConnectType", "Network"),
                new XElement("ConnectParams",
                    new XElement("NetworkParams",
                        new object[] {
                            //TOCHANGE
                            new XAttribute("Type", "Network"),
                            new XAttribute("Address", ip),
                            new XAttribute("Port",port)
                            })),
                new XElement("DecimalDigits", "2"),
                new XElement("DataFile", "Command.xml")
                )
            );

            //DeviceList.Element("ConnectParams").Add(new XElement("NetworkParams", new object[] {
            //                                            //TOCHANGE
            //                                            new XAttribute("Type", "Network"),
            //                                            new XAttribute("Address", ip),
            //                                            new XAttribute("Port",port)
            //                                            }));
            return DeviceList;
        }


        /// <summary>
        /// TaskX  
        /// </summary>
        /// <param name="TaskID"></param>
        /// <returns></returns>
        public XElement GetTaskX(string TaskID, string DataFile = "DeviceList.xml", string OutputFile = "TaskResult.xml")
        {
            XElement Task = new XElement("MTTask",
                                //TOCHANGE
                                new XElement("TaskID", TaskID),
                                new XElement("TaskType", "0"),
                                //???
                                new XElement("TaskAction", "123"),

                                //TOCHANGE
                                new XElement("DataFile", DataFile),
                                new XElement("OutputFile", OutputFile)
                    );

            return Task;
        }

        /// <summary>
        /// 生成command
        /// </summary>
        /// <param name="CommandID">命令字编号，在每台秤的命令列表中必须唯一</param>
        /// <param name="CommandText">命令字，如Item</param>
        /// <param name="Control">命令控制字：Update：更新数据。Delete：删除指定数据。DeleteAll：删除全部数据。Read：读取当前数据。ReadAll：读取所有数据。其他所有Control节点均与此相同</param>
        /// <param name="ClearData">标志下发前是否清空数据，即是否先把秤内对应数据清空后再下发，仅在命令控制字为Write或Update时有效</param>
        /// <param name="DataFile">以文件方式调用时，存放命令字数据文件名</param>
        /// <returns></returns>
        public XElement GetCommandX(string CommandID, string CommandText = "Item", string Control = "Update", bool ClearData = false, string DataFile = "Data.xml")
        {
            //if (string.IsNullOrEmpty(CommandID))
            //{
            //    throw new Exception("CommandID 不能为空");
            //}
            XElement Command = new XElement("Commands",
                    new XElement("Command",
                        new XElement("CommandText", CommandText),
                        new XElement("CommandID", CommandID),
                        new XElement("Control", Control),
                        new XElement("ClearData", ClearData),
                        //
                        new XElement("DataFile", DataFile)
                ));
            return Command;
        }


        /// <summary>
        /// 获取data item
        /// </summary>
        /// <param name="PLU"></param>
        /// <returns></returns>
        public XElement GetItem(string PLU, string commodityName, string price, string indate, string tare, string barcode, string type)
        {
            XElement Item = new XElement("Item");

            Item.Add(new XElement("PLU", PLU)); //must
            Item.Add(new XElement("DepartmentID"));
            //这里是货号 用来存我们的barcode
            Item.Add(new XElement("AlternativeItemIDs"));
            Item.Add(new XElement("Descriptions"));
            Item.Add(new XElement("Dates"));
            Item.Add(new XElement("ItemGroupID"));
            Item.Add(new XElement("CategoryIDs"));
            Item.Add(new XElement("Tares"));
            Item.Add(new XElement("ItemPrices")); //商品价格列表  可以存放多个商品价格   目前我们这边只要一个价格
            Item.Add(new XElement("Taxes"));
            Item.Add(new XElement("Ingredients"));
            Item.Add(new XElement("Ingredients"));
            Item.Add(new XElement("LabelFormats"));
            Item.Add(new XElement("Barcodes"));
            Item.Add(new XElement("NutritionInformation"));
            Item.Add(new XElement("FixedQuantity"));
            Item.Add(new XElement("TraceInfoID"));
            Item.Add(new XElement("PriceRule"));
            Item.Add(new XElement("Images"));
            Item.Add(new XElement("StaggerPrices"));

            Item.Element("Descriptions").Add(Description(CommodityName: commodityName, ID: "0"));
            Item.Element("ItemPrices").Add(ItemPrice(price, type));
            Item.Element("Dates").Add(DateOffset(indate));
            Item.Element("Tares").Add(TareID(tare));
            Item.Element("LabelFormats").Add(LabelFormatID());
            //Item.Element("Barcodes").Add(BarcodeID(barcode));
            Item.Element("AlternativeItemIDs").Add(AlternativeItemID(barcode));
            return Item;
        }

        private XElement AlternativeItemID(string barcode)
        {
            barcode = GetLastStr(barcode, 6);
            return new XElement("AlternativeItemID", barcode);
        }

        public XElement LabelFormatID()
        {
            XElement LabelFormatID = new XElement("Description",
                new object[] {
                    new XAttribute("Index", "0"),
                    });
            LabelFormatID.SetValue("1");
            return LabelFormatID;
        }

        public XElement Description(string CommodityName, string ID, string Language = "zho", string Type = "ItemName")
        {
            XElement description = new XElement("Description",
                new object[] {
                    new XAttribute("Type", Type),
                    new XAttribute("ID", ID),
                    new XAttribute("Language", Language) });
            description.SetValue(CommodityName);
            return description;
        }

        public XElement ItemPrice(string price, string type, string index = "0", string UnitOfMeasureCode = "KGM", bool PriceOverrideFlag = false, bool DiscountFlag = false, string Currency = "CNY")
        {
            string Quantity = "";
            if (type == "0")
            {
                UnitOfMeasureCode = "KGM";
                Quantity = "0";
            }
            else
            {
                UnitOfMeasureCode = "PCS";
                Quantity = "1";
            }
            XElement itemPrice = new XElement("ItemPrice",
                new object[] {
                    new XAttribute("Index", index),
                    new XAttribute("UnitOfMeasureCode", UnitOfMeasureCode),
                    new XAttribute("PriceOverrideFlag", PriceOverrideFlag),
                    new XAttribute("Quantity",Quantity), //这里有可能会变成坑
                    new XAttribute("Currency",Currency)
                });
            itemPrice.SetValue(price);
            return itemPrice;
        }

        //这里应该设置的是条码格式 按照序号提前送进机器中
        //public XElement BarcodeID(string barcodeStyle)
        //{

        //    return new XElement("BarcodeID", barcode);
        //}

        #region 获取后几位数 public string GetLastStr(string str,int num)
        /// <summary>
        /// 获取后几位数
        /// </summary>
        /// <param name="str">要截取的字符串</param>
        /// <param name="num">返回的具体位数</param>
        /// <returns>返回结果的字符串</returns>
        private string GetLastStr(string str, int num)
        {
            int count = 0;
            if (str.Length > num)
            {
                count = str.Length - num;
                str = str.Substring(count, num);
            }
            return str;
        }
        #endregion

        //这里设保质期
        public XElement DateOffset(string day)
        {
            //保质期 用indate
            XElement DateOffset = new XElement("DateOffset", new object[] { new XAttribute("Type", "SellBy"), new XAttribute("UnitOfOffset", "day"), new XAttribute("PrintFormat", "YYMMDD") });
            if (string.IsNullOrEmpty(day))
            {
                day = "0";
            }
            DateOffset.SetValue(day);
            return DateOffset;
        }

        public XElement TareID(string tare)
        {
            return new XElement("TareID", tare);
        }








    }
}
