using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Newtonsoft.Json;
using System.Runtime.Serialization;

using DeviceBridge.Files;
using DeviceBridge.Serialization;

/// @cond PRIVATE

namespace DeviceBridge.Tests
{

    
    public class UnitTest_PrivateStorageAccess : UnitTest
    {
        const string TAG = "UnitTest::PrivateStorageAccess >> ";

        const string TESTFILE_DIRECTORY = "";
        const string TESTFILE_NAME = "testfile";
        const string TESTFILE_EXTENSION = "txt";
        const string FILE_PATH = TESTFILE_DIRECTORY + TESTFILE_NAME + "." + TESTFILE_EXTENSION;
        
        const string SERIALIZED_CONTENT = "Mailand oder Madrid, hauptsache Italien!";



        public override void beginUnitTest()
        {
            if(logAll)
            {
                Debug.Log(TAG + "start test");
            }
            adapter.SendCommand(Commands.READ_PRIVATE_FILE, TESTFILE_NAME, onCreatedFile, onFailedToCreateFile);
        }


        //  test create file
    
        private void onCreatedFile(IDeviceResponse response)
        {
            if(logAll)
            {
                Debug.Log(TAG + "created testfile.. response=\n" + AdapterUtil.FormatResponse(response) );
                Debug.Log(TAG + "next test: write to the file...");
            }
            

            TestFile testFile = new TestFile(SERIALIZED_CONTENT);
            PrivateFile file = testFile.PrepareForWrite();
            adapter.SendCommand(Commands.WRITE_PRIVATE_FILE, file.ToJson(), onWrittenFile, onFailedToWriteFile);
        }


        private void onFailedToCreateFile(IDeviceResponse response)
        {
            Debug.LogError(TAG + "failed to create testfile.. response=\n" + AdapterUtil.FormatResponse(response) );
            finishTest(false);
        }

        //  test write file

        private void onWrittenFile(IDeviceResponse response)
        {
            if(logAll)
            {
                Debug.Log(TAG + "written to testfile.. response=\n" + AdapterUtil.FormatResponse(response) );
                Debug.Log(TAG + "next test: read the written file...");
            }

            adapter.SendCommand(Commands.READ_PRIVATE_FILE, FILE_PATH, onReadFile, onFailedToReadFile);
        }
        private void onFailedToWriteFile(IDeviceResponse response)
        {
            Debug.LogError(TAG + "failed to write testfile.. response=\n" + AdapterUtil.FormatResponse(response) );
            finishTest(false);
        }


        //  test read file

        private void onReadFile(IDeviceResponse response)
        {
            if(logAll)
            {
                Debug.Log(TAG + "has read testfile.. response=\n" + AdapterUtil.FormatResponse(response) );
                Debug.Log(TAG + "next test: deserialize content...");
            }

            TestFile deserializedFile;
            if(SerializableData.TryParse<TestFile>(response.content, out deserializedFile))
            {
                if(logAll)
                {
                    Debug.Log(TAG + "successfully deserialized testfile.. content=[" + deserializedFile.testString + "]" );
                }
                adapter.SendCommand(Commands.DELETE_PRIVATE_FILE, FILE_PATH, onDeleteFile, onFailedToDeleteFile);
            }
            else
            {
                Debug.LogError(TAG + "failed to deserialize testfile!" );
                finishTest(false);
            }
        }
        private void onFailedToReadFile(IDeviceResponse response)
        {
            Debug.LogError(TAG + "failed to read testfile.. response=\n" + AdapterUtil.FormatResponse(response) );
            finishTest(false);
        }


        //  test delete file

        private void onDeleteFile(IDeviceResponse response)
        {
            if(logAll)
            {
                Debug.Log(TAG + "deleted testfile.. response=\n" + AdapterUtil.FormatResponse(response) );
                finishTest(true);
            }
        }
        private void onFailedToDeleteFile(IDeviceResponse response)
        {
            Debug.LogError(TAG + "failed to delete testfile.. response=\n" + AdapterUtil.FormatResponse(response) );
            finishTest(false);
        }




        [DataContract]
        public class TestFile : SerializedFile
        {
            public override string fileName { get { return TESTFILE_NAME; } }
            public override string filePath { get { return TESTFILE_DIRECTORY; } }
            public override string fileExtension { get { return TESTFILE_EXTENSION; } }


            [DataMember]
            public string testString;

            public TestFile() {}
            public TestFile(string content) { testString = content; }

        }
    }
}

/// @endcond