using UnityEngine;
using System.Collections;

public class ServerURLS {

    public const string baseurl = "https://alkottab.co/AlkottabFurniture/api/";

    public const string GetProductsInCategory = "product/search"; // to be added to the above link then we use the parmenters in the body

    public const string GetCategoryNumber = "category/list";  // get the number of category

    public const string GetAds = "ad/list";

    public const string GetProfsInProfCategory = "professional/search";

    public const string GetprofCategoryNumber = "professional-category/list";

    public const string GetProductSuppliers = "supplier/list";

    public const string GetNotifications = "notification/list";

    public const string GetIdeas = "idea/search";

    public const string GetProductByID = "product/get";
   
    public const string GetIdeaByID = "idea/get";

}
