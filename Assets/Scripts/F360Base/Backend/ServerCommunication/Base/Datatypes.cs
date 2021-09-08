using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace F360.Backend
{

    public struct ServerFile
    {
        public string directory;
        public string filename;

        public ServerFile(string dir, string file)
        {
            this.directory = dir;
            this.filename = file;
        }
    }

    //-----------------------------------------------------------------------------------------------



}