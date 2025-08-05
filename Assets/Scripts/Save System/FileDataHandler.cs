//using System.Collections;
//using System.Collections.Generic;
//using System.IO;
//using UnityEngine;

//public class FileDataHandler
//{
//    private string fullPath;


//    public FileDataHandler(string dateDirPath, string dataFileName)
//    {
//        fullPath = Path.Combine(dateDirPath, dataFileName);
//    }

//    public void SaveData(GameData gameData)
//    {
//        try
//        {
//            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));

//            string dataToSave = JsonUtility.ToJson(gameData, true);

//            using (FileStream steam = new FileStream(fullPath))
//        }
//    }
//}
