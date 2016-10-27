//-------------------------------------------﻿﻿﻿
// YU Xinbo  
// bruce.xb.yu@connect.polyu.hk      
// PhD Candidate 
// in the Hong Kong PolyTechnical University   
//-------------------------------------------
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Kinect;

namespace Kinectv2DataCollection
{
    class SaveDataAsJson
    {
        public SaveDataAsJson()
        {
            CreateText();
        }

        public void WriteDataset(string datasets)
        {
            string path = GetFolderPath();
            string filepath = Path.Combine(path, "JointsDatasets.json");
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(filepath, true))
            {
                file.WriteLine(datasets);
            }
        }

        public void DeleteLastCha()
        {
            string path = GetFolderPath();
            string filepath = Path.Combine(path, "JointsDatasets.json");
            byte[] contents = File.ReadAllBytes(filepath);
            FileStream fileStream = File.OpenWrite(filepath);
            //using (var fileStream = new FileStream(filepath, FileMode.Open, FileAccess.ReadWrite))
            //{

            fileStream.Write(contents, 0, contents.Length - 1);
            fileStream.Close();
            //}
        }

        private string GetFolderPath()
        {
            string folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "KinectData");
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
            return folder;
        }

        /// <summary>
        /// Create the json file
        /// </summary>
        private void CreateText()
        {
            string path = GetFolderPath();
            string filepath = Path.Combine(path, "JointsDatasets.json");
            if (!File.Exists(filepath))
            {
                using (System.IO.FileStream fs = System.IO.File.Create(filepath))
                {
                    //for (byte i = 0; i < 100; i++)
                    //{
                    //    fs.WriteByte(i);
                    //}
                }
            }
        }
    }
}
