using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace F360
{


    
    [DataContract]
    public class MainCategories
    {
        public const string FILENAME = "categories";


        [DataMember]
        public string title;
        [DataMember]
        public List<Category> categories;
        Category[] main;

        private Category promo = new Category(-1, -1, "Promo", "Promo");
        bool hasSubgroups;

        private static MainCategories instance;
        public static MainCategories Deserialize(string json)
        {
            try {
                instance = JsonConvert.DeserializeObject<MainCategories>(json);
                foreach(var c in instance.categories) {
                    if(c.Subgroup >= 0) {
                        instance.hasSubgroups = true;
                        break;
                    }
                }
                instance.main = instance.categories.Where(x=> x.isMainCategory()).ToArray();

                /*var b = new System.Text.StringBuilder("Loaded Categories: ");
                foreach(var main in instance.main)
                {
                    b.Append("\n" + main.ToString());
                    foreach(var sub in ListGroup(main))
                    {
                        b.Append("\n\t" + sub.ToString());
                    }
                }
                Debug.Log(b);*/
                return instance;
            }
            catch(System.Exception ex) {
                Debug.LogError("Error loading MainCategory config: " + ex.Message);
            }
            return null;
        }
        public static bool wasLoaded() { return instance != null; }

        public static Category AddSubcategory(int group, int subgroup, string title, string description="")
        {
    //        Debug.Log("...try add subgroup:: " + group + " / " + subgroup + " (" + (instance != null) + ") title=[" + title + "]"); 
            if(instance != null)
            {
                var c = GetByGroup(group, subgroup);
                if(c != null && string.IsNullOrEmpty(c.Title))
                {
                    c = new Category(group, subgroup, title, description);
                    instance.categories.Add(c);
                }
          //      else Debug.LogWarning("----> [" + (c != null ? c.title : "null") + "]");
                return c;
            }
            return null;
        }

        public static void Order()
        {
            instance.categories = instance.categories.OrderBy(x=> x.Group*100 + x.Subgroup).ToList();
        }

        public static string GetTitle() { return instance != null ? instance.title : "UNKNOWN LABEL"; }

        public static int Count { get { return instance.categories.Count; } }

        public static IEnumerable<Category> GetAll() { return instance.categories; }
        public static IEnumerable<Category> GetMain() { return instance.main; }
        
        public static Category GetByID(int id)
        {
            if(instance != null)
            {
                if(instance.promo != null && id == instance.promo.ID)
                {
                    return instance.promo;
                }
                else
                {
                    for(int i = 0; i < instance.categories.Count; i++)
                    {
                        if(instance.categories[i].ID == id)
                        return instance.categories[i];
                    }
                }
                throw new System.Exception("main category with id=[" + id + "] not found!");
            }
            return null;
        }

        public static Category GetByTitle(string title)
        {
            if(instance != null)
            {
                title = title.ToLower();
                for(int i = 0; i < instance.categories.Count; i++)
                {
                    if(instance.categories[i].Title.ToLower() == title)
                    return instance.categories[i];
                }
                throw new System.Exception("main category with title=[" + title + "] not found!");
            }
            return null;
        }

        public static Category GetByGroup(int group, int subgroup=-1)
        {
            if(instance != null)
            {
                for(int i = 0; i < instance.categories.Count; i++)
                {
                    if(instance.categories[i].Group == group && 
                        (subgroup == -1 || instance.categories[i].Subgroup == subgroup))
                    {
                        return instance.categories[i];
                    }
                }
            }
            return null;
        }

        public static IEnumerable<Category> ListGroup(Category root)
        {   
            return ListGroup(root.Group);
        }
        public static IEnumerable<Category> ListGroup(int group)
        {
            if(instance != null)
            {
                for(int i = 0; i < instance.categories.Count; i++)
                {
                    if(instance.categories[i].Group == group 
                        && instance.categories[i].isSubCategory())
                    {
                        yield return instance.categories[i];
                    }
                }
            }
        }

        public static bool Exists(int group, int subgroup=-1)
        {
            if(instance != null)
            {   
                for(int i = 0; i < instance.categories.Count; i++)
                {
                    if(instance.categories[i].Group == group && (subgroup == -1 || instance.categories[i].Subgroup == subgroup))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public override string ToString()
        {
            var b = new System.Text.StringBuilder("MainCategories");
            foreach(var c in categories)
            {
                if(hasSubgroups)
                {
                    if(c.Subgroup == -1)
                        b.Append("\n\t[" + c.Group.ToString() + "] " + c.Title);
                    else
                        b.Append("\n\t\t[" + c.Group.ToString() + "." + c.Subgroup.ToString() + "] " + c.Title);
                }
                else
                {
                    b.Append("\n\t[" + c.ID.ToString() + "] " + c.Title);
                }
            }
            return b.ToString();
        }
    }

    [DataContract]
    public class Category
    {
        public static int GetIDFromGroups(int group, int subgroup=0)
        {
            if(subgroup <= 0)
            {
                return group;
            }
            else
            {
                return group * 100 + subgroup;
            }
        }

        #pragma warning disable


        [DataMember]
        int id;

        [DataMember]
        string title;

        [DataMember]
        int group;

        [DataMember]
        int subgroup;

        [DataMember]
        string description;

        [DataMember]
        string pathToTrainerVideo;

        #pragma warning restore


        public int ID { get { return id; } }
        public string Title { get { return title; } }
        public int Group { get { return group; } }
        public int Subgroup { get { return subgroup; } }
        public string Description { get { return description; } }
        public string PathToTrainerVideo { get { return pathToTrainerVideo; } }
        

        public string GroupLabel { 
            get { 
                if(isMainCategory()) return Group.ToString() + ".";
                else return Group.ToString() + "." + Subgroup.ToString();
            }
        }

        public bool isMainCategory() { return Subgroup <= 0 && Group > 0; }
        public bool isSubCategory() { return Subgroup > 0; }

        public bool isSubgroupOf(Category parent)
        {
            return parent != this && this.Group == parent.ID;
        }

        public Category(int group, int subgroup, string title, string description)
        {
            this.id = GetIDFromGroups(group, subgroup);
            this.group = group;
            this.subgroup = subgroup;
            this.title = title;
            this.description = description;
        }

        public override string ToString()
        {
            return "Category[" + ID.ToString() + "] { grouping: " + Group.ToString() + "." + Subgroup.ToString() + ":: " + Title + "}";
        }

    }

}

