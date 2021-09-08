using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace F360.Users
{
    public class StudentRepo : UserRepo<F360Student>
    {

        public static StudentRepo Instance 
        { 
            get 
            {
                if(instance == null)
                {
                    instance = CreateRepo<StudentRepo>();
                }
                return instance;
            } 
        }
        static StudentRepo instance;

        //-----------------------------------------------------------------------------------------------------------------

        public override Backend.Database Database { get { return Backend.Database.Students; } }
        public override int Identity { get { return IdentityUtil.FromUserType(UserType.Student); } }

        /*public F360Student GetLenderOfDevice(string deviceID)
        {
            foreach(var user in repository.GetAll())
            {
                var student = user as F360Student;
                if(student != null && student.RENTED_DEVICE == deviceID)
                {
                    return student;
                }
            }
            return null;
        }*/

        //-----------------------------------------------------------------------------------------------------------------

        protected override string FileName { get { return "cs_"; } }
        
        protected override F360Student creatorFunc(Backend.Messages.SC_UserInfo info)
        {
            return User.Create<F360Student>(info.name, "", info.mail);
        }
        protected override bool serverUpdateFunc(F360Student user, Backend.Messages.SC_UserInfo info)
        {
            return true;
        }

        

        protected override void OnRepoAvailable()
        {
            if(!repository.Exists(DEV_ACC_NAME))
            {
                createDevStudentAccount();
            }
            else
            {
//                Debug.Log("Student dev account exists already... " + repository.Get(DEV_ACC_NAME).PrintPendingChanges());
            }

            if(!repository.Exists(DEGNER_ACC_NAME))
            {
                createDegnerDevAccount();
            }
        }

        protected override void onAfterUpdateFromServer()
        {
           
        }

        //-----------------------------------------------------------------------------------------------------------------


        const string DEV_ACC_NAME = ActiveUser.DEV_ACC; //"joris.drobka@gmx.de";
        const string DEV_ACC_PASS = ActiveUser.DEV_PW; //"pahelia11&";


        void createDevStudentAccount()
        {
            /*var acc = F360Student.Create<F360Student>(DEV_ACC_NAME, DEV_ACC_PASS);
            if(repository.Add(acc))
            {
                Debug.Log("Created Dev Student Account! " + acc.GetType() + " {" + acc.ID + "}");
            }*/
        }



        const string DEGNER_ACC_NAME = "FS360gService";
        const string DEGNER_ACC_PASS = "123456";

        void createDegnerDevAccount()
        {
            /*var acc = F360Student.Create<F360Student>(DEGNER_ACC_NAME, DEGNER_ACC_PASS);
            if(repository.Add(acc))
            {
//                Debug.Log("Created Degner Student Account! " + acc.PrintPendingChanges());
            }*/
        }


    }

}

