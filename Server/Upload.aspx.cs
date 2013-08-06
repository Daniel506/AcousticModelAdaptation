using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using Ionic.Zip;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Net;

public partial class Upload : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        string vTitle = "";
        string vDesc = "";
        
        if (!string.IsNullOrEmpty(Request.Form["title"]))
        {
            vTitle = Request.Form["title"];
        }
        if (!string.IsNullOrEmpty(Request.Form["description"]))
        {
            vDesc = Request.Form["description"];
        }

        string FilePath = Server.MapPath(vTitle);
        string WavPath = Server.MapPath("~/wav/" + vTitle);

        HttpFileCollection MyFileCollection = Request.Files;
        if (MyFileCollection.Count > 0)
        {
            // Save the File
            MyFileCollection[0].SaveAs(FilePath);
        }
    }

}