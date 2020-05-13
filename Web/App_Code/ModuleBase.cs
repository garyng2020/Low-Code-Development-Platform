using System;
using System.Data;
using System.Data.OleDb;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.HtmlControls;
using System.ComponentModel;
using RO.Common3;
using RO.Common3.Data;
using RO.Facade3;
using System.Text;
using System.Configuration;
using System.Security.Cryptography;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using System.Net;
using System.IO;
using System.Web.Script.Serialization;
using System.Web.Configuration;
using System.Security.Cryptography.X509Certificates;
using RoboCoder.WebControls;

namespace RO.Web
{
    public class InvokeResult
    {
        public string ContentDisposition;
        public string ContentType;
        public byte[] content;
    }
    public class MenuNode
	{
		public string ParentQId { get; set; }
		public string QId { get; set; }
		public string MenuId { get; set; }
		public string ParentId { get; set; }
		public string MenuText { get; set; }
		public string NavigateUrl { get; set; }
		public string QueryStr { get; set; }
		public string IconUrl { get; set; }
        public string Popup { get; set; }
        public string GroupTitle { get; set; }
		public List<MenuNode> Children { get; set; }
		public bool Selected { get; set; }
		public int Level { get; set; }
		public MenuNode()
		{
		}

        public string ToUnorderList(string iconCss, string urlCss, string groupCss, string itemCss, string inPathCss, string levelCss, string selectedCss, string nodeCss, string firstCss, bool isFirst, string lastCss, bool isLast, string baseCss, string sessionId)
        {
            List<string> subList = new List<string>();
            List<string> appliedCss = new List<string>() { itemCss, 
                                                            string.Format("{0}_{1}",levelCss,Level),
                                                            GroupTitle == "Y" || Children.Count > 0 ? groupCss : "",
                                                            Selected ? inPathCss : "",
                                                            Selected && Children.Count == 0 ? selectedCss : "",
                                                            isFirst ? firstCss : "",
                                                            isLast ? lastCss : ""
                                                            };

            int i = 0, cnt = Children.Count;

            foreach (MenuNode mt in Children)
            {
                subList.Add(mt.ToUnorderList(iconCss, urlCss, groupCss, itemCss, inPathCss, levelCss, selectedCss, nodeCss, firstCss, i == 0, lastCss, i == cnt - 1, baseCss, sessionId));
                i = i + 1;
            }
            return
            string.Format("<li class=\"{2}\"><div class=\"{3}\">{0}</div>{1}</li>",
            string.Format("<a href=\"{0}{1}{2}{7}\"" + (Popup=="Y" ? " target=\"_blank\"" : "") + " class=\"{4}\" {6} {8} >{5}{3}</a>",
                NavigateUrl.StartsWith("javascript:") ? "" : string.IsNullOrEmpty(NavigateUrl) ? "javascript:void(0)" : NavigateUrl,
                    NavigateUrl.StartsWith("javascript:") || string.IsNullOrEmpty(QueryStr) ? "" : NavigateUrl.Contains("?") ? QueryStr : "?" + QueryStr,
                    NavigateUrl.Contains("&id=") || NavigateUrl.Contains("?id=") || QueryStr.Contains("&id=") || QueryStr.Contains("?id=")
                    || NavigateUrl.ToLower().StartsWith("http:") || NavigateUrl.ToLower().StartsWith("https:") || NavigateUrl.ToLower().StartsWith("ftp:")
                    || NavigateUrl.ToLower().StartsWith("mailto:") || NavigateUrl.ToLower().StartsWith("file:")
                    || NavigateUrl.ToLower().StartsWith("javascript:") ? "" : (string.IsNullOrEmpty(NavigateUrl) ? "" : "&id=" + MenuId),
                    MenuText,
                    urlCss + (string.IsNullOrEmpty(NavigateUrl) ? " menuNoClick" : ""),
                    string.IsNullOrEmpty(IconUrl) ? "" : string.Format("<img src=\"{0}\" class=\"{1}\"/>", IconUrl, iconCss),
                    NavigateUrl.StartsWith("javascript:") ? string.Format("onclick=\"{0};return false;\"", NavigateUrl.Replace("javascript:", "").Replace("\"", "'")) : "",
                    NavigateUrl.Contains("&ssd=") || NavigateUrl.Contains("?ssd=") || QueryStr.Contains("&ssd=") || QueryStr.Contains("?ssd=")
                    || NavigateUrl.ToLower().StartsWith("http:") || NavigateUrl.ToLower().StartsWith("https:") || NavigateUrl.ToLower().StartsWith("ftp:")
                    || NavigateUrl.ToLower().StartsWith("mailto:") || NavigateUrl.ToLower().StartsWith("file:")
                    || NavigateUrl.ToLower().StartsWith("javascript:") || string.IsNullOrEmpty(sessionId)
                    || string.IsNullOrEmpty(NavigateUrl) ? "" : string.Format("&ssd={0}", sessionId),
                    (NavigateUrl.ToLower().StartsWith("http") || NavigateUrl.ToLower().StartsWith("ftp")) ? " target = '_blank' " : ""
                    )
                    ,
                subList.Count == 0 ? "" : string.Format("<ul class=\"{1}\">{0}</ul>", string.Join("", subList.ToArray()), baseCss + "Sub"),
                nodeCss,
                string.Join(" ", appliedCss.ToArray())
                );
        }
    }

    public class ZipMultiDocRequest
    {
        public List<string> scr;
        public List<List<string>> cols;
    }

    public class ZipEmbeddedDocRequest
    {
        public List<string> scr;
        public List<List<string>> cols;
    }
    public class ZipDownloadRequest
    {
        public string zN;
        public long e;
        public List<ZipMultiDocRequest> md;
        public List<ZipEmbeddedDocRequest> ed;
    }
	public class PgStateAdapter : System.Web.UI.Adapters.PageAdapter
	{
		public override PageStatePersister GetStatePersister()
		{
			return new SessionPageStatePersister(this.Page);
		}
	}

	public class ModuleBase : UserControl
	{
		private string PageUrlBase;
        private string IntPageUrlBase;

        private string findControlId;

		private const String KEY_SystemsList = "Cache:SystemsList";
		private const String KEY_SystemsDict = "Cache:SystemsDict";
		private const String KEY_SysConnectStr = "Cache:SysConnectStr";
		private const String KEY_AppConnectStr = "Cache:AppConnectStr";
        private const String KEY_SystemAbbr = "Cache:SystemAbbr";
        private const String KEY_DesDb = "Cache:DesDb";
		private const String KEY_AppDb = "Cache:AppDb";
		private const String KEY_AppUsrId = "Cache:AppUsrId";
		private const String KEY_AppPwd = "Cache:AppPwd";
        private const String KEY_SysAdminEmail = "Cache:SysAdminEmail";
        private const String KEY_SysAdminPhone = "Cache:SysAdminPhone";
        private const String KEY_SysCustServEmail = "Cache:SysCustServEmail";
        private const String KEY_SysCustServPhone = "Cache:SysCustServPhone";
        private const String KEY_SysCustServFax = "Cache:SysCustServFax";
        private const String KEY_SysWebAddress = "Cache:SysWebAddress";

		private const String KEY_CacheLUser = "Cache:LUser";
		private const String KEY_CacheLPref = "Cache:LPref";
        private const String KEY_CacheLImpr = "Cache:LImpr";
		private const String KEY_CacheLCurr = "Cache:LCurr";
		private const String KEY_CacheCPrj = "Cache:CPrj";
		private const String KEY_CacheCSrc = "Cache:CSrc";
		private const String KEY_CacheCTar = "Cache:CTar";
		private const String KEY_CacheVMenu = "Cache:VMenu";
        private static string _ROVersion = null;
        protected static object o_lock = new object();
        protected static string ROVersion { 
            get {
                if (_ROVersion == null)
                {
                    lock (o_lock)
                    {
                        try
                        {
                            _ROVersion = (new LoginSystem()).GetRbtVersion();
                        }
                        catch
                        {
                            _ROVersion = "unknown";
                        }
                    }
                }
                return _ROVersion;
            } 
        }
		public ModuleBase()
		{
            var Request = Context.Request;
            IntPageUrlBase = (Request.IsSecureConnection ? "https://" : "http://") 
                                + Request.Url.Host 
                                + (Request.Url.IsDefaultPort ? "" : ":" + Request.Url.Port.ToString())
                                + (Request.ApplicationPath + "/").Replace("//","/");
            PageUrlBase = ResolveUrlCustom("~/", false, true);
            string xForwardedFor = Request.Headers["X-Forwarded-For"];
            string extBasePath = Config.ExtBasePath;
            string appPath = Request.ApplicationPath;

            if (!string.IsNullOrEmpty(xForwardedFor) 
                && !string.IsNullOrEmpty(extBasePath) 
                && (extBasePath??"").Length != appPath.Length 
                && !Request.Url.ToString().ToLower().Contains("msg.aspx")
                )
            {
                throw new Exception(string.Format("Server configuration issue, proxy path({0}) must be the same length as app path({1}), check extBaseUrl setting", extBasePath, appPath));
            }

                //(IsSecureConnection() || Config.EnableSsl ? @"https://" : @"http://") 
                //+ Context.Request.Url.Host 
                //+ Context.Request.ApplicationPath +  "/";
		}

		protected String UrlBase
		{
			get {return PageUrlBase;}
		}
        protected String IntUrlBase
        {
            get { return IntPageUrlBase; }
        }

        protected bool IsCrawlerBot(string userAgent)
        {
            /*
             * facebook - facebookexternalhit/1.1+(+http://www.facebook.com/externalhit_uatext.php)
             * facebook - Facebot
             * twitter - Twitterbot/1.0
             * rintagi - RintagiBot/1.0 
             * (via SelfInvoke)
             */
            Dictionary<string, string> botSignature = new Dictionary<string, string>
            {
                {"Twitterbot","Twitterbot/1.0"},
                {"facebookexternalhit","facebookexternalhit/1.1+(+http://www.facebook.com/externalhit_uatext.php)"},
                {"Facebot","Facebot"},
                {"RintagiBot","RintagiBot/1.0"},
            };
            foreach (var x in botSignature)
            {
                if (userAgent.ToLower().Contains(x.Key.ToLower())) return true;
            }
            return false;
        }

        protected DataTable SystemsDict
        {
            set
            {
                DataTable dt = value;
                Session[KEY_SystemsList] = dt;
                dt.PrimaryKey = new DataColumn[] { dt.Columns["SystemId"] };
                bool singleSQLCredential = (System.Configuration.ConfigurationManager.AppSettings["DesShareCred"] ?? "N") == "Y";
                for (int i = dt.Rows.Count; i < dt.Rows.Count; i++)
                {
                    DataRow dr = dt.Rows[i];
                    if (dr["Active"].ToString() == "N")
                    {
                        dr.Delete();
                    }
                    else if (singleSQLCredential)
                    {
                        dr["ServerName"] = Config.DesServer;
                    }
                }
                dt.AcceptChanges();
                Session[KEY_SystemsDict] = new Dictionary<byte, Dictionary<string, string>>();
                foreach (DataRow dr in dt.Rows)
                {

                    Dictionary<string, string> dict = new Dictionary<string, string>();
                    dict[KEY_SysConnectStr] = Config.GetConnStr(dr["dbAppProvider"].ToString(), singleSQLCredential ? Config.DesServer : dr["ServerName"].ToString(), dr["dbDesDatabase"].ToString(), "", singleSQLCredential ? Config.DesUserId : dr["dbAppUserId"].ToString());
                    dict[KEY_AppConnectStr] = Config.GetConnStr(dr["dbAppProvider"].ToString(), singleSQLCredential ? Config.DesServer : dr["ServerName"].ToString(), dr["dbAppDatabase"].ToString(), "", singleSQLCredential ? Config.DesUserId : dr["dbAppUserId"].ToString());
                    dict[KEY_SystemAbbr] = dr["SystemAbbr"].ToString();
                    dict[KEY_DesDb] = dr["dbDesDatabase"].ToString();
                    dict[KEY_AppDb] = dr["dbAppDatabase"].ToString();
                    dict[KEY_AppUsrId] = singleSQLCredential ? Config.DesUserId : dr["dbAppUserId"].ToString();
                    dict[KEY_AppPwd] = singleSQLCredential ? Config.DesPassword : dr["dbAppPassword"].ToString();
                    try { dict[KEY_SysAdminEmail] = dr["AdminEmail"].ToString(); } catch { dict[KEY_SysAdminEmail] = string.Empty; }
                    try { dict[KEY_SysAdminPhone] = dr["AdminPhone"].ToString(); } catch { dict[KEY_SysAdminPhone] = string.Empty; }
                    try { dict[KEY_SysCustServEmail] = dr["CustServEmail"].ToString(); } catch { dict[KEY_SysCustServEmail] = string.Empty; }
                    try { dict[KEY_SysCustServPhone] = dr["CustServPhone"].ToString(); } catch { dict[KEY_SysCustServPhone] = string.Empty; }
                    try { dict[KEY_SysCustServFax] = dr["CustServFax"].ToString(); } catch { dict[KEY_SysCustServFax] = string.Empty; }
                    try { dict[KEY_SysWebAddress] = dr["WebAddress"].ToString(); } catch { dict[KEY_SysWebAddress] = string.Empty; }
                    ((Dictionary<byte, Dictionary<string, string>>)Session[KEY_SystemsDict])[byte.Parse(dr["SystemId"].ToString())] = dict;
                }
            }
        }
        protected DataTable SystemsList
		{
            get { try { return (DataTable)(Session[KEY_SystemsList]); } catch { return (null); } }
            set { Session[KEY_SystemsList] = value; }
		}

		protected String SysConnectStr(byte SystemId)
		{
			try {return ((Dictionary<byte, Dictionary<string, string>>)Session[KEY_SystemsDict])[SystemId][KEY_SysConnectStr];} catch {return string.Empty;}
		}

		protected String AppConnectStr(byte SystemId)
		{
			try {return ((Dictionary<byte, Dictionary<string, string>>)Session[KEY_SystemsDict])[SystemId][KEY_AppConnectStr];} catch {return string.Empty;}
		}

		protected String DesDb(byte SystemId)
		{
			try {return ((Dictionary<byte, Dictionary<string, string>>)Session[KEY_SystemsDict])[SystemId][KEY_DesDb];} catch {return string.Empty;}
		}

		protected String AppDb(byte SystemId)
		{
			try {return ((Dictionary<byte, Dictionary<string, string>>)Session[KEY_SystemsDict])[SystemId][KEY_AppDb];} catch {return string.Empty;}
		}

		protected String AppUsrId(byte SystemId)
		{
			try {return ((Dictionary<byte, Dictionary<string, string>>)Session[KEY_SystemsDict])[SystemId][KEY_AppUsrId];} catch {return string.Empty;}
		}

		protected String AppPwd(byte SystemId)
		{
			try {return ((Dictionary<byte, Dictionary<string, string>>)Session[KEY_SystemsDict])[SystemId][KEY_AppPwd];} catch {return string.Empty;}
		}

        protected String SysAdminEmail(byte SystemId)
        {
            try { return ((Dictionary<byte, Dictionary<string, string>>)Session[KEY_SystemsDict])[SystemId][KEY_SysAdminEmail]; } catch { return string.Empty; }
        }

        protected String SysAdminPhone(byte SystemId)
        {
            try { return ((Dictionary<byte, Dictionary<string, string>>)Session[KEY_SystemsDict])[SystemId][KEY_SysAdminPhone]; } catch { return string.Empty; }
        }

        protected String SysCustServEmail(byte SystemId)
        {
            try { return ((Dictionary<byte, Dictionary<string, string>>)Session[KEY_SystemsDict])[SystemId][KEY_SysCustServEmail]; } catch { return string.Empty; }
        }

        protected String SysCustServPhone(byte SystemId)
        {
            try { return ((Dictionary<byte, Dictionary<string, string>>)Session[KEY_SystemsDict])[SystemId][KEY_SysCustServPhone]; } catch { return string.Empty; }
        }

        protected String SysCustServFax(byte SystemId)
        {
            try { return ((Dictionary<byte, Dictionary<string, string>>)Session[KEY_SystemsDict])[SystemId][KEY_SysCustServFax]; } catch { return string.Empty; }
        }

        protected String SysWebAddress(byte SystemId)
        {
            try { return ((Dictionary<byte, Dictionary<string, string>>)Session[KEY_SystemsDict])[SystemId][KEY_SysWebAddress]; } catch { return string.Empty; }
        }

		protected LoginUsr LUser
		{
			get {try {return (LoginUsr)(Session[KEY_CacheLUser]);} 
				 catch {return (null);}
			}
			set {if (null == value) {Session.Remove(KEY_CacheLUser);} 
				 else {Session[KEY_CacheLUser] = value;}
			}
		}

		protected UsrPref LPref
		{
			get {try {return (UsrPref)(Session[KEY_CacheLPref]);} 
				 catch {return (null);}
			}
			set {if (null == value) {Session.Remove(KEY_CacheLPref);} 
				 else {Session[KEY_CacheLPref] = value;}
			}
		}

		protected UsrImpr LImpr
		{
			get {try {return (UsrImpr)(Session[KEY_CacheLImpr]);} 
				 catch {return (null);}
			}
			set {if (null == value) {Session.Remove(KEY_CacheLImpr);} 
				 else {Session[KEY_CacheLImpr] = value;}
			}
		}

		protected UsrCurr LCurr
		{
			get {try {return (UsrCurr)(Session[KEY_CacheLCurr]);} 
				 catch {return (null);}
			}
			set {if (null == value) {Session.Remove(KEY_CacheLCurr);} 
				 else {Session[KEY_CacheLCurr] = value;}
			}
		}

        protected CurrPrj CPrj
        {
            get
            {
                try { return (CurrPrj)(Session[KEY_CacheCPrj]); }
                catch { return (null); }
            }
            set
            {
                if (null == value) { Session.Remove(KEY_CacheCPrj); }
                else { Session[KEY_CacheCPrj] = value; }
                bool singleSQLCredential = (System.Configuration.ConfigurationManager.AppSettings["DesShareCred"] ?? "N") == "Y";
                string RedirectProjectRoot = System.Configuration.ConfigurationManager.AppSettings["RedirectProjectRoot"];

                if (singleSQLCredential)
                {
                    value.SrcDesServer = Config.DesServer;
                    value.SrcDesUserId = Config.DesUserId;
                    value.SrcDesPassword = Config.DesPassword;
                    value.TarDesServer = Config.DesServer;
                    value.TarDesUserId = Config.DesUserId;
                    value.TarDesPassword = Config.DesPassword;

                }
                if (!string.IsNullOrEmpty(RedirectProjectRoot))
                {
                    string[] redirect = RedirectProjectRoot.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToArray();
                    if (redirect.Length == 2)
                    {
                        value.DeployPath = value.DeployPath.Replace(redirect[0], redirect[1]);
                        value.SrcClientProgramPath = value.SrcClientProgramPath.Replace(redirect[0], redirect[1]);
                        value.SrcRuleProgramPath = value.SrcRuleProgramPath.Replace(redirect[0], redirect[1]);
                        value.SrcWsProgramPath = value.SrcWsProgramPath.Replace(redirect[0], redirect[1]);
                        value.TarClientProgramPath = value.TarClientProgramPath.Replace(redirect[0], redirect[1]);
                        value.TarRuleProgramPath = value.TarRuleProgramPath.Replace(redirect[0], redirect[1]);
                        value.TarWsProgramPath = value.TarWsProgramPath.Replace(redirect[0], redirect[1]);
                    }
                }
            }
        }

        protected CurrSrc CSrc
        {
            get
            {
                try { return (CurrSrc)(Session[KEY_CacheCSrc]); }
                catch { return (null); }
            }
            set
            {
                if (null == value) { Session.Remove(KEY_CacheCSrc); }
                else { Session[KEY_CacheCSrc] = value; }
                bool singleSQLCredential = (System.Configuration.ConfigurationManager.AppSettings["DesShareCred"] ?? "N") == "Y";
                if (singleSQLCredential)
                {
                    value.SrcServerName = Config.DesServer;
                    value.SrcDbServer = Config.DesServer;
                    value.SrcDbUserId = Config.DesUserId;
                    value.SrcDbPassword = Config.DesPassword;
                }
            }
        }

        protected CurrTar CTar
        {
            get
            {
                try { return (CurrTar)(Session[KEY_CacheCTar]); }
                catch { return (null); }
            }
            set
            {
                if (null == value) { Session.Remove(KEY_CacheCTar); }
                else { Session[KEY_CacheCTar] = value; }
                bool singleSQLCredential = (System.Configuration.ConfigurationManager.AppSettings["DesShareCred"] ?? "N") == "Y";
                if (singleSQLCredential)
                {
                    value.TarServerName = Config.DesServer;
                    value.TarDbServer = Config.DesServer;
                    value.TarDbUserId = Config.DesUserId;
                    value.TarDbPassword = Config.DesPassword;
                }

            }
        }

        protected DataView VMenu
		{
			get
			{
				try { return ((DataTable)(Session[KEY_CacheVMenu])).DefaultView; } 
				 catch {return (null);}
			}
			set
			{
				if (null == value) {Session.Remove(KEY_CacheVMenu);}
				else { Session[KEY_CacheVMenu] = value.Table; }
			}
		}

        protected bool AllowEdit(Dictionary<string, DataRow> dAuth, string columnName)
        {
            return (dAuth[columnName]["ColReadOnly"].ToString() != "Y");
        }

        protected bool AllowRowEdit(DataTable dtAuthRow, string key)
        {
            DataRow dr = dtAuthRow.Rows[0];
            if (dr["AllowUpd"].ToString() == "N" && !string.IsNullOrEmpty(key)) return false;
            else if (dr["AllowAdd"].ToString() == "N" && string.IsNullOrEmpty(key)) return false;
            else return true;
        }

        protected bool AllowRowDel(DataTable dtAuthRow, string key)
        {
            DataRow dr = dtAuthRow.Rows[0];
            if (dr["AllowDel"].ToString() == "N" && !string.IsNullOrEmpty(key)) return false;
            else return true;
        }

        protected bool GetBool(string vv)
		{
			if (vv == "Y") return true; else return false;
		}

		protected string SetBool(bool bb)
		{
			if (bb) return "Y"; else return "N";
		}

        // TableCell tc is obsolete and will be removed:
		protected void SetCriBehavior(WebControl cc, TableCell tc, Label cl, DataRow dl)
		{
            if (dl["DisplayMode"].ToString().ToLower() == "starrating")
            {
                string max = dl["RowSize"].ToString();
                if (string.IsNullOrEmpty(max)) max = "5";
                cc.Attributes["Behaviour"] = "Rating";
                cc.Attributes["MaxRating"] = max;
            }
            else if (dl["DisplayMode"].ToString().ToLower() == "progressbar")
            {
                string max = dl["RowSize"].ToString();
                if (string.IsNullOrEmpty(max)) max = "10";
                string step = "1";
                step = (int.Parse(max) / 10).ToString();
                cc.Attributes["Behaviour"] = "Slider";
                cc.Attributes["Min"] = "0";
                cc.Attributes["Max"] = max;
                cc.Attributes["Step"] = step;
            }
            else if (dl["DisplayMode"].ToString().Contains("DateTime"))
            {
                cc.Attributes["Behaviour"] = "DateTime";
            }
            else if (dl["DisplayMode"].ToString().Contains("Date"))
            {
                cc.Attributes["Behaviour"] = "Date";
                cc.Attributes["DateFormat"] = dl["DisplayMode"].ToString();
            }
            if (cc != null && dl["ColumnSize"].ToString() != string.Empty)
			{
				if (dl["DisplayName"].ToString() == "Calendar")
				{
					cc.Font.Size = new FontUnit(dl["ColumnSize"].ToString() + "px");
				}
				else
				{
					cc.Style.Add("width", dl["ColumnSize"].ToString() + "px");      // Not max-width because 100% width will be assigned on mobile.
				}
			}
			if (cc != null && dl["RowSize"].ToString() != string.Empty)
			{
				if (dl["DisplayName"].ToString() == "ListBox")
				{
					ListBox lb = (ListBox)cc;
                    if (lb != null) { lb.Height = int.Parse(dl["RowSize"].ToString()) * 16 + 7; lb.Rows = int.Parse(dl["RowSize"].ToString()); }
				}
                else if (dl["DisplayMode"].ToString().ToLower() != "starrating")
				{
					cc.Height = new Unit(dl["RowSize"].ToString() + "px");
				}
			}
			if (cl != null)
			{
				if (dl["ColumnHeader"].ToString().Trim() != string.Empty) { cl.Text = dl["ColumnHeader"].ToString() + ":"; } else { cl.Text = string.Empty; }
                if (dl["RequiredValid"].ToString() == "Y" && "ImagePopUp,ImageLink,ImageButton,CheckBox".IndexOf(dl["DisplayName"].ToString()) < 0)
                {
                    if (cl.Text.EndsWith(":")) cl.Text = cl.Text.Replace(":", string.Empty) + "<span class=\"Mandatory\">" + Config.MandatoryChar + "</span>:";
                    else cl.Text = cl.Text + "<span class=\"Mandatory\">" + Config.MandatoryChar + "</span>";
                }
            }
            if (dl.Table.Columns.Contains("DefaultValue") && !string.IsNullOrEmpty(dl["DefaultValue"].ToString()))
            {
                ScriptManager.RegisterStartupScript(cc, cc.GetType(), "SetDefaultValue_" + cc.ClientID,
                    "$(document).ready(function(){$('#" + cc.ClientID + "').val(" + dl["DefaultValue"].ToString().Replace("'", "\'") + ");});", true);
            }
            return;
		}

        // For Document imaging, ImageButton, and textarea:
        protected void SetFoldBehavior(WebControl cc, DataRow dd, Control p1, Label cl, Control p2, WebControl ib, WebControl ch, DataRow dl, RequiredFieldValidator rfv, RegularExpressionValidator rev, RangeValidator rv)
		{
			if (dd["ColVisible"].ToString() != "Y" && p2 != null) { p2.Visible = false; }
            if (ib != null)
            {
                GridView gv = cc as GridView;
                if (dd["ColVisible"].ToString() != "Y" || dd["ColReadOnly"].ToString() == "Y")
                {
                    ib.Visible = false;
                    if (gv != null)
                    {
                        gv.Columns[3].Visible = false;
                        gv.Columns[4].Visible = false;
                    }
                }
                else
                {
                    ib.Visible = true;
                    if (gv != null)
                    {
                        gv.Columns[3].Visible = true;
                        gv.Columns[4].Visible = true;
                    }
                }
            }
            SetFoldBehavior(cc, dd, p1, cl, ch, dl, rfv, rev, rv);
		}

        // For other than button, ImageButton or upload:
        protected void SetFoldBehavior(WebControl cc, DataRow dd, Control p1, Label cl, Control p2, WebControl ch, DataRow dl, RequiredFieldValidator rfv, RegularExpressionValidator rev, RangeValidator rv)
        {
            if (dd["ColVisible"].ToString() != "Y" && p2 != null) { p2.Visible = false; }
            SetFoldBehavior(cc, dd, p1, cl, ch, dl, rfv, rev, rv);
        }

        protected void SetFoldBehavior(WebControl cc, DataRow dd, Control tc, Label cl, WebControl ch, DataRow dl, RequiredFieldValidator rfv, RegularExpressionValidator rev, RangeValidator rv)
		{
			if (dd["ColVisible"].ToString() != "Y")
			{
				cc.Visible = false;
                if (tc != null)
                {
                    tc.Visible = false;
                    if (ch != null && tc.Parent.Controls.Contains(ch)) { tc.Parent.Visible = false; };
                }
                if (ch != null) { ch.Visible = false; }
			}
			else // Visible.
			{
                try
                {
                    if (dl["IgnoreConfirm"].ToString() != "Y")
                    {
                        var prop = cc.GetType().GetProperty("AutoPostBack");
                        if (prop != null && ((bool)prop.GetValue(cc, null))) cc.Attributes["onChange"] = "CurrConfirmMsg=$(this).attr('title');";
                    }
                }
                catch { }
                if (dl["DisplayMode"].ToString().ToLower() == "starrating")
                {
                    string max = dl["ColumnHeight"].ToString();
                    if (string.IsNullOrEmpty(max)) max = "5";
                    cc.Attributes["Behaviour"] = "Rating";
                    cc.Attributes["MaxRating"] = max;
                }
                else if (dl["DisplayMode"].ToString().ToLower() == "progressbar")
                {
                    string max = dl["ColumnHeight"].ToString();
                    if (string.IsNullOrEmpty(max)) max = "10";
                    string step = "1";
                    step = (int.Parse(max) / 10).ToString();
                    cc.Attributes["Behaviour"] = "Slider";
                    cc.Attributes["Min"] = "0";
                    cc.Attributes["Max"] = max;
                    cc.Attributes["Step"] = step;
                }
                else if (dl["DisplayMode"].ToString().Contains("DateTime"))
                {
                    cc.Attributes["Behaviour"] = "DateTime";
                }
                else if (dl["DisplayMode"].ToString().Contains("Date"))
                {
                    cc.Attributes["Behaviour"] = "Date";
                    cc.Attributes["DateFormat"] = dl["DisplayMode"].ToString();
                }
                if (dl["ColumnSize"].ToString() != string.Empty)
				{
					if (dl["DisplayName"].ToString() == "Calendar")
					{
						cc.Font.Size = new FontUnit(dl["ColumnSize"].ToString() + "px");
					}
					else
					{
                        if (dl["DisplayMode"].ToString().ToLower() == "radiobuttonlist")
                        {
                            cc.Style.Add("max-width", (int.Parse(dl["ColumnSize"].ToString()) - 2).ToString() + "px");
                        }
                        else
                        {
                            if (Request.Browser.Browser == "IE" && Request.Browser.MajorVersion <= 8 && false)
                                cc.Style.Add("width", dl["ColumnSize"].ToString() + "px !important");
                            else
                                cc.Style.Add("max-width", dl["ColumnSize"].ToString() + "px");
                        }
                        if (ch != null && (dl["DisplayMode"].ToString() == "Document" || dl["DisplayName"].ToString() == "ImageButton"))
                        {
                            if (int.Parse(dl["ColumnSize"].ToString()) > 300)
                            {
                                ch.Style.Add("max-width", dl["ColumnSize"].ToString() + "px");
                            }
                            else { ch.Style.Add("max-width", "300px"); }
                        }
					}
				}
                if (dl["ColumnHeight"].ToString() != string.Empty && dl["DisplayMode"].ToString().ToLower() != "starrating")
				{
					if (dl["DisplayMode"].ToString() == "Document")
					{
						GridView gv = cc as GridView;
						if (gv != null) { gv.PageSize = int.Parse(dl["ColumnHeight"].ToString()); }
                        ch.Height = new Unit(((int.Parse(dl["ColumnHeight"].ToString()) + 1) * 29 + 22).ToString() + "px");
                    }
					else if (dl["DisplayName"].ToString() == "DataGrid")
					{
						DataGrid dg = cc as DataGrid;
						if (dg != null) { dg.PageSize = int.Parse(dl["ColumnHeight"].ToString()); }
					}
					else if (dl["DisplayName"].ToString() == "ListBox")
					{
						ListBox lb = cc as ListBox;
                        if (lb != null) { lb.Height = int.Parse(dl["ColumnHeight"].ToString()) * 16 + 7; lb.Rows = int.Parse(dl["ColumnHeight"].ToString()); }
                    }
                    else if (dl["DisplayName"].ToString() == "ImageButton")
                    {
                        cc.Style.Add("max-height", dl["ColumnHeight"].ToString() + "px");
                    }
                    else
					{
						cc.Height = new Unit(dl["ColumnHeight"].ToString() + "px");
					}
				}
				if (dl["ToolTip"].ToString().Trim() != string.Empty) {cc.ToolTip = dl["ToolTip"].ToString();} else {cc.ToolTip = string.Empty;}
				if (cl != null)
				{
					if (dl["ColumnHeader"].ToString().Trim() != string.Empty)
                    {
                        cl.Text = dl["ColumnHeader"].ToString() + ":";
                    }
                    else { cl.Text = string.Empty; }
				}
                else if (dl["DisplayName"].ToString() == "Label") { ((Label)cc).Text = dl["ColumnHeader"].ToString(); }
				if (cc.GetType().FullName.Equals("System.Web.UI.WebControls.Button"))
				{
					Button bn = cc as Button; if (bn != null) { bn.Text = dl["ColumnHeader"].ToString(); }
				}
                if (dd["ColReadOnly"].ToString() == "Y" && dl["DisplayMode"].ToString() != "Document")
				{

                    if (cc is TextBox) { ((TextBox)cc).Enabled = false; ((TextBox)cc).TabIndex = -1; } else { cc.Enabled = false; }
					if ("ImagePopUp,ImageLink,ImageButton,CheckBox,Label".IndexOf(dl["DisplayName"].ToString()) < 0)
					{
						cc.BackColor = System.Drawing.Color.FromName(Config.ReadOnlyBColor);
					}
                    if (ch != null) { ch.Visible = false; }
				}
                else
                {
                    cc.Enabled = true;
                    if ("ImagePopUp,ImageLink,ImageButton,CheckBox,Label".IndexOf(dl["DisplayName"].ToString()) < 0)
                    {
                        cc.BackColor = System.Drawing.Color.White;
                        if (ch != null) { ch.Visible = true; }
                    }
                    if (dl["RequiredValid"].ToString() == "Y" && "ImagePopUp,ImageLink,ImageButton,CheckBox".IndexOf(dl["DisplayName"].ToString()) < 0)
                    {
                        if (cl != null) { cl.Text = cl.Text.Replace(":", string.Empty) + "<span class=\"Mandatory\">" + Config.MandatoryChar + "</span>:"; }
                    }
                    if (cc is RoboCoder.WebControls.EncryptedTextBox)
                    {
                        RoboCoder.WebControls.EncryptedTextBox t = (RoboCoder.WebControls.EncryptedTextBox)cc;
                        t.EncryptionKey = Config.SecuredColumnKey;
                    }
                }
				if (dl["RequiredValid"].ToString() == "Y" && rfv != null)
				{
					rfv.ErrorMessage = dl["ErrMessage"].ToString();
				}
				if (dl["MaskValid"].ToString() != null && dl["MaskValid"].ToString().Trim() != string.Empty && rev != null)
				{
					rev.ErrorMessage = dl["ErrMessage"].ToString();
				}
				if (dl["RangeValidType"].ToString() != null && dl["RangeValidType"].ToString().Trim() != string.Empty && rv != null)
				{
					rv.ErrorMessage = dl["ErrMessage"].ToString();
				}
            }
			return;
		}

		protected void SetGridBehavior(GridView gv, DataTable dtAuth, DataTable dtLabel, int initCnt, int finCnt)
		{
			if (dtAuth != null && dtLabel != null)
			{
				for (int ii = initCnt; ii < finCnt; ii++)
				{
					gv.Columns[ii - initCnt].HeaderText = dtLabel.Rows[ii]["ColumnHeader"].ToString();
					gv.Columns[ii - initCnt].HeaderStyle.CssClass = "GrdHead";
					if (dtLabel.Rows[ii]["ColumnJustify"].ToString() == "R")
					{
						gv.Columns[ii - initCnt].HeaderStyle.HorizontalAlign = HorizontalAlign.Right;
						gv.Columns[ii - initCnt].ItemStyle.HorizontalAlign = HorizontalAlign.Right;
						gv.Columns[ii - initCnt].FooterStyle.HorizontalAlign = HorizontalAlign.Right;
					}
					else if (dtLabel.Rows[ii]["ColumnJustify"].ToString() == "C")
					{
						gv.Columns[ii - initCnt].HeaderStyle.HorizontalAlign = HorizontalAlign.Center;
						gv.Columns[ii - initCnt].ItemStyle.HorizontalAlign = HorizontalAlign.Center;
						gv.Columns[ii - initCnt].FooterStyle.HorizontalAlign = HorizontalAlign.Center;
					}
				}
			}
			return;
		}

        protected void SetGridEnabled(ListViewItem lvi, DataTable dtAuth, DataTable dtLabel, int initCnt)
        {
            string columnName = "";
            WebControl cc = null;
            WebControl et = null;
            RequiredFieldValidator rfv = null;
            RegularExpressionValidator rev = null;
            RangeValidator rv = null;
            if (dtAuth != null && dtLabel != null)
            {
                int ii = initCnt;
                int rCnt = dtAuth.Rows.Count - 1;
                while (ii <= rCnt)
                {
                    if (dtLabel.Rows[ii]["TableId"].ToString() != null && dtLabel.Rows[ii]["TableId"].ToString().Trim() != string.Empty)
                    {
                        columnName = dtLabel.Rows[ii]["ColumnName"].ToString() + dtLabel.Rows[ii]["TableId"].ToString();
                    }
                    else
                    {
                        columnName = dtLabel.Rows[ii]["ColumnName"].ToString();
                    }
                    if (dtAuth.Rows[ii]["ColVisible"].ToString() == "Y")
                    {
                        cc = (WebControl)lvi.FindControl("c" + columnName);
                        if (cc != null)
                        {
                            DataRow dl = dtLabel.Rows[ii];
                            try
                            {
                                if (dl["IgnoreConfirm"].ToString() != "Y")
                                {
                                    var prop = cc.GetType().GetProperty("AutoPostBack");
                                    if (prop != null && ((bool)prop.GetValue(cc, null))) cc.Attributes["onChange"] = "CurrConfirmMsg=$(this).attr('title');";
                                }
                            }
                            catch { }
                            if (dl["DisplayMode"].ToString().ToLower() == "encryptedtextbox")
                            {
                                EncryptedTextBox etb = (EncryptedTextBox)cc;
                                etb.EncryptionKey = Config.SecuredColumnKey;
                            }
                            if (dl["DisplayMode"].ToString().ToLower() == "multiline")
                            {
                                et = (WebControl)lvi.FindControl("c" + columnName + "E");
                            }
                            if (dl["DisplayMode"].ToString().ToLower() == "starrating")
                            {
                                string max = dl["ColumnHeight"].ToString();
                                if (string.IsNullOrEmpty(max)) max = "5";
                                cc.Attributes["Behaviour"] = "Rating";
                                cc.Attributes["MaxRating"] = max;
                            }
                            else if (dl["DisplayMode"].ToString().ToLower() == "progressbar")
                            {
                                string max = dl["ColumnHeight"].ToString();
                                if (string.IsNullOrEmpty(max)) max = "10";
                                string step = "1";
                                try { step = (int.Parse(max) / 10).ToString(); }
                                catch { };
                                cc.Attributes["Behaviour"] = "Slider";
                                cc.Attributes["Min"] = "0";
                                cc.Attributes["Max"] = max;
                                cc.Attributes["Step"] = step;
                            }
                            else if (dl["DisplayMode"].ToString().Contains("DateTime"))
                            {
                                cc.Attributes["Behaviour"] = "DateTime";
                            }
                            else if (dl["DisplayMode"].ToString().Contains("Date"))
                            {
                                cc.Attributes["Behaviour"] = "Date";
                                cc.Attributes["DateFormat"] = dl["DisplayMode"].ToString();
                            }
                        }
                        if (dtAuth.Rows[ii]["ColReadOnly"].ToString() == "Y")
                        {
                            cc.Enabled = false;
                            cc.BorderStyle = BorderStyle.None;
                            if ("ImagePopUp,ImageLink,ImageButton,CheckBox".IndexOf(dtLabel.Rows[ii]["DisplayName"].ToString()) < 0)
                            {
                                cc.BackColor = System.Drawing.Color.FromName(Config.ReadOnlyBColor);
                            }
                            if (et != null) { et.Visible = false; }     // No need to test for cc invisibility.
                        }
                    }
                    if (dtLabel.Rows[ii]["RequiredValid"].ToString() == "Y" && "ImagePopUp,ImageLink,ImageButton,CheckBox".IndexOf(dtLabel.Rows[ii]["DisplayName"].ToString()) < 0)
                    {
                        rfv = (RequiredFieldValidator)lvi.FindControl("cRFV" + columnName);
                        if (rfv != null)
                        {
                            rfv.ErrorMessage = dtLabel.Rows[ii]["ErrMessage"].ToString();
                        }
                    }
                    if (dtLabel.Rows[ii]["MaskValid"].ToString() != null && dtLabel.Rows[ii]["MaskValid"].ToString().Trim() != string.Empty)
                    {
                        rev = (RegularExpressionValidator)lvi.FindControl("cREV" + columnName);
                        if (rev != null)
                        {
                            rev.ErrorMessage = dtLabel.Rows[ii]["ErrMessage"].ToString();
                        }
                    }
                    if (dtLabel.Rows[ii]["RangeValidType"].ToString() != null && dtLabel.Rows[ii]["RangeValidType"].ToString().Trim() != string.Empty)
                    {
                        rv = (RangeValidator)lvi.FindControl("cRV" + columnName);
                        if (rv != null)
                        {
                            rv.ErrorMessage = dtLabel.Rows[ii]["ErrMessage"].ToString();
                        }
                    }
                    ii = ii + 1;
                }
            }
            return;
        }

        // For backward compatibility only:
		protected string GetExpression(string sFind, DataTable dtAuth, int initCnt)
		{
			string sExpression = "";
			if (dtAuth != null)
			{
				int ii = initCnt;
				int rCnt = dtAuth.Rows.Count - 1;
				while (ii <= rCnt)
				{
					if (dtAuth.Rows[ii]["ColVisible"].ToString() == "Y")
					{
						if (sExpression != "")
						{
							sExpression = sExpression + " or ";
						}
						sExpression = sExpression + "convert(" + dtAuth.Rows[ii]["ColName"].ToString() + ",'System.String') like '*" + sFind.Replace("'","''") + "*'";
					}
					ii = ii + 1;
				}
			}
			return sExpression;
		}

        /* Dataview.RowFilter cannot handle wildcard in the middle */
        protected string GetExpression(string sFind, DataTable dtAuth, int initCnt, string col)
        {
            int ii = col != string.Empty ? 0 : initCnt;
            string sExpression = string.Empty;
            if (dtAuth != null)
            {
                if (col != string.Empty)
                {
                    ii = ii + int.Parse(col);
                    if (dtAuth.Rows[ii]["DisplayMode"].ToString().Contains("Date"))
                    {
                        DateTime searchDate;
                        bool isDate = DateTime.TryParse(sFind, out searchDate);

                        if (isDate)
                        {
                            sExpression = string.Format(System.Globalization.CultureInfo.InvariantCulture.DateTimeFormat, "{0} = #{1}#", dtAuth.Rows[ii]["ColName"].ToString(), searchDate);
                        }
                        else
                        {
                            sExpression = string.Format("convert({0},'System.String') like '*{1}*'", dtAuth.Rows[ii]["ColName"].ToString(), sFind.Replace("*", string.Empty).Replace("%", string.Empty).Replace("'", "''"));
                        }
                    }
                    else
                    {
                        sExpression = "convert(" + dtAuth.Rows[ii]["ColName"].ToString() + ",'System.String') like '*" + sFind.Replace("*", string.Empty).Replace("%", string.Empty).Replace("'", "''") + "*'";
                    }
                }
                else
                {
                    int rCnt = dtAuth.Rows.Count - 1;
                    while (ii <= rCnt)
                    {
                        if (dtAuth.Rows[ii]["ColVisible"].ToString() == "Y")
                        {
                            if (sExpression != "") { sExpression = sExpression + " or "; }
                            if (dtAuth.Rows[ii]["DisplayMode"].ToString().Contains("Date"))
                            {
                                DateTime searchDate;

                                bool isDate = DateTime.TryParse(sFind, out searchDate);

                                if (isDate)
                                {
                                    sExpression = sExpression + string.Format(System.Globalization.CultureInfo.InvariantCulture.DateTimeFormat, "{0} = #{1}#", dtAuth.Rows[ii]["ColName"].ToString(), searchDate);
                                }
                                else
                                {
                                    sExpression = sExpression + string.Format("convert({0},'System.String') like '*{1}*'", dtAuth.Rows[ii]["ColName"].ToString(), sFind.Replace("*", string.Empty).Replace("%", string.Empty).Replace("'", "''"));
                                }
                            }
                            else
                            {
                                sExpression = sExpression + "convert(" + dtAuth.Rows[ii]["ColName"].ToString() + ",'System.String') like '*" + sFind.Replace("*", string.Empty).Replace("%", string.Empty).Replace("'", "''") + "*'";
                            }
                        }
                        ii = ii + 1;
                    }
                }
            }
            return sExpression;
        }

        // This should be called on every postback to keep the images alive:
        //protected void MkLinkHolder(PlaceHolder cLinkHolder, string PageObjId, string SectionCd, string LinkTypeCd)
        //{
        //    if (LLink != null && !string.IsNullOrEmpty(PageObjId))
        //    {
        //        Panel dv = new Panel();
        //        Panel tb = new Panel();
        //        Panel tr = new Panel();
        //        Panel td = new Panel();
        //        HtmlControl ul = new HtmlGenericControl("ul");
        //        HtmlControl li;
        //        HyperLink hl;
        //        ImageButton ib;
        //        DataView dvLink = new DataView(LLink);
        //        dvLink.RowFilter = "PageObjId=" + PageObjId;
        //        dvLink.Sort = "PageLnkOrd";
        //        bool bNothing = true;
        //        if (LinkTypeCd == "LGO" && dvLink.Count == 0)
        //        {
        //            DataRow dr = dvLink.Table.NewRow();
        //            dr["PageObjId"] = PageObjId;
        //            dr["SectionCd"] = SectionCd;
        //            dr["LinkTypeCd"] = "LGO";
        //            dvLink.Table.Rows.Add(dr);
        //        }
        //        bool bFirstLi = true;
        //        foreach (DataRowView drv in dvLink)
        //        {
        //            string sPrefix = string.Empty;
        //            string sPageLnkPath = string.Empty;
        //            string sPageLnkRef = string.Empty;
        //            if (LinkTypeCd == "LGO")
        //            {
        //                drv["PageLnkTxt"] = string.Empty;   // Must be image;
        //                //if (string.IsNullOrEmpty(drv["PageLnkImg"].ToString())) { drv["PageLnkImg"] = Config.LoginImage; }
        //                if (string.IsNullOrEmpty(drv["PageLnkRef"].ToString())) { drv["PageLnkRef"] = Config.SslUrl; }
        //            }
        //            if (!string.IsNullOrEmpty(drv["PageLnkRef"].ToString()))
        //            {
        //                /* Try starting from root directory */
        //                if ("file,mail,http,java".IndexOf(drv["PageLnkRef"].ToString().ToLower().Trim().Left(4)) < 0 && !drv["PageLnkRef"].ToString().StartsWith("../") && !drv["PageLnkRef"].ToString().StartsWith("~/")) { sPrefix = "~/"; }
        //                sPageLnkPath = sPrefix + (new Regex("\\?.*$").Replace(drv["PageLnkRef"].ToString().Trim(), ""));
        //                sPageLnkRef = sPrefix + drv["PageLnkRef"].ToString().Trim();
        //                /* If not found, use current directory */
        //                if (sPageLnkPath.StartsWith("~/") && !System.IO.File.Exists(Server.MapPath(sPageLnkPath))) { sPageLnkRef = sPageLnkRef.Substring(2); }
        //            }
        //            ib = new ImageButton();
        //            if (!string.IsNullOrEmpty(drv["PageLnkImg"].ToString()))
        //            {
        //                if (!string.IsNullOrEmpty(drv["PageLnkId"].ToString())) { ib.ID = "cPageLnk" + drv["PageLnkId"].ToString(); }
        //                string ImageUrl = drv["PageLnkImg"].ToString().Trim();
        //                if (!drv["PageLnkImg"].ToString().StartsWith("~/") && !drv["PageLnkImg"].ToString().ToLower().StartsWith("http"))
        //                {
        //                    ImageUrl = "~/" + ImageUrl;
        //                }
        //                string ImageUrlPath = new Regex("\\?.*$").Replace(ImageUrl, string.Empty);
        //                if (ImageUrlPath.StartsWith("~/") && !System.IO.File.Exists(Server.MapPath(ImageUrlPath))) ImageUrl = ImageUrl.Substring(2);
        //                ib.ImageUrl = ImageUrl;
        //                if (drv["PageLnkAlt"] != null && drv["PageLnkAlt"].ToString().Trim() != string.Empty)
        //                {
        //                    ib.Attributes["onmouseover"] = "MouseOverEffect(this,'" + drv["PageLnkAlt"].ToString().Trim().Replace("~/", string.Empty) + "');";
        //                    ib.Attributes["onmouseout"] = "MouseOverEffect(this,'" + drv["PageLnkImg"].ToString().Trim().Replace("~/", string.Empty) + "');";
        //                }
        //                if (!string.IsNullOrEmpty(drv["PageLnkRef"].ToString()))
        //                {
        //                    if (drv["Popup"].ToString() == "Y" && !drv["PageLnkRef"].ToString().ToLower().StartsWith("javascript:"))
        //                    {
        //                        // Javascript cannot handle relative path:
        //                        string wostr = "'";
        //                        if ("file,mail,http".IndexOf(drv["PageLnkRef"].ToString().Trim().Substring(0, 4)) < 0) { wostr = wostr + UrlBase; }
        //                        wostr = wostr + drv["PageLnkRef"].ToString().Trim() + "','PageLnk" + drv["PageLnkId"].ToString() + "'";
        //                        ib.Attributes["onclick"] = "window.open(" + wostr + ",'resizable=yes,toolbar=yes,scrollbars=yes,width=700,height=400'); return false;";
        //                    }
        //                    else
        //                    {
        //                        if (sPageLnkRef.ToLower().StartsWith("javascript:"))
        //                            ib.Attributes["onclick"] = new Regex("^$javascript:", RegexOptions.IgnoreCase).Replace(sPageLnkRef, "");
        //                        else
        //                            ib.PostBackUrl = sPageLnkRef;
        //                    }
        //                    ib.Enabled = true;
        //                    if (LinkTypeCd == "CRS")    // Carousel.
        //                    {
        //                        ib.Attributes.Add("style", "width:100%; cursor:pointer;" + drv["PageLnkCss"].ToString());
        //                    }
        //                    else
        //                    {
        //                        ib.Attributes.Add("style", "cursor:pointer;" + drv["PageLnkCss"].ToString());
        //                    }
        //                }
        //                else
        //                {
        //                    ib.Enabled = false;
        //                    if (LinkTypeCd == "CRS")    // Carousel.
        //                    {
        //                        ib.Attributes.Add("style", "width:100%; cursor:default;" + drv["PageLnkCss"].ToString());
        //                    }
        //                    else
        //                    {
        //                        ib.Attributes.Add("style", "cursor:default;" + drv["PageLnkCss"].ToString());
        //                    }
        //                }
        //            }
        //            hl = new HyperLink();
        //            if (!string.IsNullOrEmpty(drv["PageLnkTxt"].ToString()))
        //            {
        //                hl.Text = drv["PageLnkTxt"].ToString().Trim();
        //                if (!string.IsNullOrEmpty(drv["PageLnkRef"].ToString()))
        //                {
        //                    if (drv["Popup"].ToString() == "Y" && (!sPageLnkRef.ToLower().StartsWith("javascript:"))) { hl.Target = "_blank"; }
        //                    if (sPageLnkRef.ToLower().StartsWith("javascript:"))
        //                        hl.Attributes["onclick"] = new Regex("^$javascript:", RegexOptions.IgnoreCase).Replace(sPageLnkRef, "");
        //                    else
        //                        hl.NavigateUrl = sPageLnkRef;
        //                    hl.Enabled = true; hl.Attributes.Add("style", "cursor:pointer;" + drv["PageLnkCss"].ToString());
        //                }
        //                else
        //                {
        //                    hl.Enabled = false; hl.Attributes.Add("style", "cursor:default;" + drv["PageLnkCss"].ToString());
        //                }
        //            }
        //            if (LinkTypeCd == "CRS")    // Carousel.
        //            {
        //                li = new HtmlGenericControl("li");
        //                if (bFirstLi)
        //                {
        //                    ul.Attributes.Add("class", "slides");
        //                    ul.Attributes.Add("style", drv["PageObjCss"].ToString());
        //                    li.Attributes.Add("class", "flex-active-slide");
        //                    bFirstLi = false;
        //                }
        //                else
        //                {
        //                    li.Attributes.Add("style", "display:none;");
        //                }
        //                if (!string.IsNullOrEmpty(ib.ImageUrl)) { li.Controls.Add(ib); }
        //                if (!string.IsNullOrEmpty(hl.Text)) { li.Controls.Add(hl); }
        //                ul.Controls.Add(li);
        //            }
        //            else
        //            {
        //                if (bFirstLi)
        //                {
        //                    dv.Attributes.Add("style", drv["PageObjCss"].ToString());
        //                    bFirstLi = false;
        //                }
        //                if (!string.IsNullOrEmpty(ib.ImageUrl)) { td.Controls.Add(ib); }
        //                if (!string.IsNullOrEmpty(hl.Text)) { td.Controls.Add(hl); }
        //            }
        //            bNothing = false;
        //        }
        //        if (!bNothing)
        //        {
        //            cLinkHolder.Controls.Clear();
        //            if (LinkTypeCd == "CRS")
        //            {
        //                cLinkHolder.Controls.Add(ul);
        //            }
        //            else
        //            {
        //                tr.CssClass = "r-tr";
        //                tr.Controls.Add(td);
        //                tb.CssClass = "r-table";
        //                tb.Controls.Add(tr);
        //                dv.Controls.Add(tb);
        //                cLinkHolder.Controls.Add(dv);
        //            }
        //        }
        //    }
        //}

        protected string ReformatErrMsg(string msg)
        {
            // Reformat:
            if (LUser != null && msg != null)
            {
                byte sid;
                string sm = msg;
                Regex re = new Regex("(\\|[0-9]{1,5}\\|)");
                if (re.IsMatch(msg))
                {
                    Match m = re.Match(msg);
                    sid = byte.Parse(m.Captures[0].Value.Replace("|", string.Empty));
                    sm = re.Replace(sm, string.Empty);
                }
                else { sid = 0; }
                msg = (new AdminSystem()).GetMsg(sm, LUser.CultureId, LUser.TechnicalUsr, SysConnectStr(sid), AppPwd(sid)).Replace("\r\n", "\r").Replace("\r", "</br>");
            }
            if (!string.IsNullOrEmpty(msg) && msg.IndexOf("\n") >= 0)
            {
                int rCount = 0;
                int cCount = 0;
                System.Text.StringBuilder sr = new System.Text.StringBuilder();
                System.Collections.Generic.Dictionary<string, string> Keys = new System.Collections.Generic.Dictionary<string, string>();
                string[] lb = { "\n" };
                foreach (string line in msg.Split(lb, StringSplitOptions.None))
                {
                    rCount = rCount + 1;
                    cCount = 0;
                    string rowCssCls = string.Format("Row{0} {1}", rCount, rCount % 2 == 0 ? "even" : "odd");
                    System.Text.StringBuilder sc = new System.Text.StringBuilder();
                    foreach (string el in line.Split('|'))
                    {
                        cCount = cCount + 1;
                        string colCssCls = string.Format("Col{0} {1}", cCount, cCount % 2 == 0 ? "even" : "odd");
                        if (cCount == 1 && !string.IsNullOrEmpty(el) && !Keys.ContainsKey(el))
                        {
                            Keys[el] = line;
                            rowCssCls = rowCssCls + " RowBreak";
                            sc.Append(string.Format("<td class='{0}'>{1}</td>", colCssCls, el));
                        }
                        else
                        {
                            sc.Append(string.Format("<td class='{0}'>{1}</td>", colCssCls, Keys.ContainsKey(el) ? "" : el));
                        }
                    }
                    sr.Append(string.Format("<tr class='{0}'>{1}</tr>", rowCssCls, sc.ToString()));
                }
                return "<div class='ErrLstContainer'><table class='ErrLst'>" + sr.ToString() + "</table></div>";
            }
            else return msg;
        }

        protected void Signout(bool redirect)
        {
            System.Web.Security.FormsAuthentication.SignOut();
            Session.Remove("CurrSystemId");
            Session.Remove("ProjectList");
            Session.Remove("CompanyList");
            SystemsList = null;
            LCurr = null;
            LUser = null;
            LPref = null;
            VMenu = null;
            if (redirect)
            {
                Session.Abandon();
                this.Redirect(Config.OrdUrl);
            }
        }

        protected void SwitchSystem(UsrCurr curr)
        {
            LCurr = curr;
            LImpr = null;
            VMenu = null;
            try
            {
                SetUsrPreference();
                SetImpersonation(LUser.UsrId);
            }
            catch { }
        }

        protected void SetImpersonation(Int32 usrId)
        {
            UsrImpr ui = null;
            Int32 companyId;
            Int32 projectId;
            if (LCurr != null)
            {
                companyId = LCurr.CompanyId; projectId = LCurr.ProjectId;
            }
            else
            {
                companyId = 0; projectId = 0;
            }
            ui = (new LoginSystem()).GetUsrImpr(usrId, companyId, projectId, LCurr.SystemId);
            if (ui != null)
            {
                if (LImpr == null)
                {
                    LImpr = ui;
                    //if (LUser.LoginName == "Anonymous") { LImpr.Cultures = LUser.CultureId.ToString(); }
                }
                else // Append:
                {
                    LImpr.Usrs = ui.Usrs;
                    LImpr.UsrGroups = ui.UsrGroups;
                    LImpr.Cultures = ui.Cultures;
                    LImpr.RowAuthoritys = ui.RowAuthoritys;
                    LImpr.Companys = ui.Companys;
                    LImpr.Projects = ui.Projects;
                    LImpr.Investors = ui.Investors;
                    LImpr.Customers = ui.Customers;
                    LImpr.Vendors = ui.Vendors;
                    LImpr.Agents = ui.Agents;
                    LImpr.Brokers = ui.Brokers;
                    LImpr.Members = ui.Members;
                    LImpr.Borrowers = ui.Borrowers;
                    LImpr.Lenders = ui.Lenders;
                    LImpr.Guarantors = ui.Guarantors;
                }
                DataTable dt = (new LoginSystem()).GetUsrImprNext(usrId);
                if (dt != null && dt.Rows.Count > 0)
                {
                    foreach (DataRow dr in dt.Rows)
                    {
                        string TestDupUsrs = (char)191 + LImpr.Usrs + (char)191;
                        if (TestDupUsrs.IndexOf((char)191 + dr["ImprUsrId"].ToString() + (char)191) < 0)
                        {
                            SetImpersonation(Int32.Parse(dr["ImprUsrId"].ToString()));
                        }
                    }
                }
            }
        }
        protected void SetUsrPreference()
        {
            LPref = (new LoginSystem()).GetUsrPref(LUser.UsrId, LCurr.CompanyId, LCurr.ProjectId, LCurr.SystemId);
        }

        protected bool IsCronInvoked()
        {
            return !string.IsNullOrEmpty(Request.QueryString["cron"]) && Request.QueryString["cron"].ToString() == Application.GetHashCode().ToString();
        }

        protected void UpdCronStatus(int jobId, bool bIsFirstTime, string LcSysConnString, string LcAppPw)
        {
            DataTable dtJob = (new AdminSystem()).GetCronJob(jobId, null, LcSysConnString, LcAppPw);
            DataRow dr = dtJob.Rows[0];
            DateTime nowUTC = DateTime.Parse(DateTime.Now.ToUniversalTime().ToString("g"));   // strip to minute for comparison.
            DateTime nextRun = Utils.GetNextRun(nowUTC, dr["Year"] as short?, dr["Month"] as byte?, dr["Day"] as byte?, dr["DayOfWeek"] as byte?, dr["Hour"] as byte?, dr["Minute"] as byte?);
            if (nowUTC == nextRun && !bIsFirstTime) nextRun = Utils.GetNextRun(nowUTC.AddMinutes(1), dr["Year"] as short?, dr["Month"] as byte?, dr["Day"] as byte?, dr["DayOfWeek"] as byte?, dr["Hour"] as byte?, dr["Minute"] as byte?);
            (new AdminSystem()).UpdCronJob(jobId, bIsFirstTime ? new DateTime(1900, 1, 1) : DateTime.Now.ToUniversalTime(), nextRun, LcSysConnString, LcAppPw);
        }

        protected void UpdCronStatus(int jobId, string LcSysConnString, string LcAppPw)
        {
            UpdCronStatus(jobId, false, LcSysConnString, LcAppPw);
        }

        protected void UpdCronStatus(string LcSysConnString, string LcAppPw)
        {
            int jobId = int.Parse(Request.QueryString["jid"].ToString());
            UpdCronStatus(jobId, LcSysConnString, LcAppPw);
        }
        protected void UpdCronStatus(string msg, string LcSysConnString, string LcAppPw)
        {
            int jobId = int.Parse(Request.QueryString["jid"].ToString());
            string url = ResolveUrlCustom(Request.RawUrl, false, true);
            (new AdminSystem()).UpdCronJobStatus(jobId, DateTime.Now.ToUniversalTime().ToString() + " - " + msg , LcSysConnString, LcAppPw);
            UpdCronStatus(jobId, LcSysConnString, LcAppPw);
        }
        protected Pair GetSSD()
        {
            string ssd = string.Empty;
            bool bFromReferrer = false;
            if (Request.QueryString["ssd"] != null && Request.QueryString["ssd"].ToString() != string.Empty)
            {
                ssd = Request.QueryString["ssd"].ToString();
            }
            else
            {
                /* Get ssd from last url for the case only for hyperlink/imagelink.  UrlReferrer is null on Window.open.  */
                System.Text.RegularExpressions.Regex re = new System.Text.RegularExpressions.Regex("ssd=[0-9]+", RegexOptions.IgnoreCase);
                System.Text.RegularExpressions.Match match = re.Match(Request.UrlReferrer != null ? Request.UrlReferrer.Query : "");
                if (match.Success) { ssd = new System.Text.RegularExpressions.Regex("[0-9]+").Match(match.Value).Value; bFromReferrer = true; }
            }
            return new Pair(ssd, bFromReferrer);
        }

        protected void ResetSSD()
        {
            Pair ssdInfo = GetSSD();
            Session.Remove("CmpPrj" + (string)ssdInfo.First);
        }

        protected void SetupSSD()
        {
            Pair ssdInfo = GetSSD();
            string ssd = ((string)ssdInfo.First ?? "").Split(new char[] { ',' }).Last();
            bool bFromReferrer = (bool)ssdInfo.Second;
            string csy = string.Empty;
            Dictionary<string, string> CmpPrj = new Dictionary<string, string>();
            bool bValidSSD = false;

            if (Request.QueryString["ssd"] != null && Request.QueryString["ssd"].ToString() != string.Empty)
            {
                ssd = Request.QueryString["ssd"].ToString().Split(new char[] { ',' }).Last();
            }
            else
            {
                /* Get ssd from last url for the case only for hyperlink/imagelink.  UrlReferrer is null on Window.open.  */
                System.Text.RegularExpressions.Regex re = new System.Text.RegularExpressions.Regex("ssd=[0-9]+", RegexOptions.IgnoreCase);
                System.Text.RegularExpressions.Match match = re.Match(Request.UrlReferrer != null ? Request.UrlReferrer.Query : "");
                if (match.Success) { ssd = new System.Text.RegularExpressions.Regex("[0-9]+").Match(match.Value).Value; bFromReferrer = true; }
            }
            if (ssd != string.Empty)
            {
                try
                {
                    CmpPrj = (Dictionary<string, string>)Session["CmpPrj" + ssd];
                    if (CmpPrj == null)
                    {
                        CmpPrj = new Dictionary<string, string>();
                        bValidSSD = false;
                    }
                    else
                        bValidSSD = true;
                }
                catch { ssd = string.Empty; }
            }
            if (!string.IsNullOrEmpty(Request.QueryString["msy"])) { csy = Request.QueryString["msy"].ToString().Split(new char[] { ',' }).Last(); }
            else if (!string.IsNullOrEmpty(Request.QueryString["csy"])) { csy = Request.QueryString["csy"].ToString().Split(new char[] { ',' }).Last(); }
            if (Request.IsAuthenticated && LUser != null)
            {
                /* Need to do these again when this program is redirected to. */
                if (!bValidSSD)
                {
                    if (ssd == string.Empty)
                    {
                        int isd = 0;
                        if (Session["SessionCnt"] != null) { isd = (int)Session["SessionCnt"]; }
                        isd = isd + 1; Session["SessionCnt"] = isd;
                        ssd = isd.ToString();
                    }
                    Session["CmpPrj" + ssd] = CmpPrj;
                    CmpPrj["cmp"] = LUser.DefCompanyId.ToString();
                    CmpPrj["prj"] = LUser.DefProjectId.ToString();
                }
                if (csy == string.Empty)
                {
                    //try { csy = GetUriKey("csy",false); }
                    //catch { }
                    //finally
                    //{
                        if (csy == string.Empty) { csy = LUser.DefSystemId.ToString(); } 
                    //}
                }
                if (LCurr != null)
                {
                    byte cSys = LCurr.SystemId;
                    string[] qSys = (Request.QueryString["csy"] ?? "").Split(new char[] { ',' },StringSplitOptions.RemoveEmptyEntries);
                    bool multiSys = Request.Path.ToLower().Contains("sqlreport.aspx") || Request.Path.ToLower().Contains("showpage.aspx");
                    if (!string.IsNullOrEmpty(Request.QueryString["msy"]) || !multiSys || qSys.Length > 1)
                    {
                        LCurr = new UsrCurr(Int32.Parse(CmpPrj["cmp"]), Int32.Parse(CmpPrj["prj"]), byte.Parse(csy), LCurr.DbId);
                        if (cSys.ToString() != csy) { SwitchSystem(LCurr); }
                    }
                }
                else
                {
                    LCurr = new UsrCurr(Int32.Parse(CmpPrj["cmp"]), Int32.Parse(CmpPrj["prj"]), byte.Parse(csy), byte.Parse(csy));
                }

                if ((!bValidSSD || bFromReferrer) && !IsPostBack)
                {
                    string strUrl = Request.RawUrl;
                    System.Collections.Specialized.NameValueCollection qs = System.Web.HttpUtility.ParseQueryString(Request.QueryString.ToString());
                    qs["ssd"] = ssd;
                    qs["msy"] = csy;
                    if (Request.HttpMethod == "POST" && Request.Form.Count > 0 && LUser != null && LUser.LoginName != "Anonymous")
                    {
                        Session["DirectPostedData"] = Request.Form;
                    }
                    if (!IsCrawlerBot(Request.UserAgent)) { this.Redirect((strUrl.IndexOf("?") < 0 ? strUrl : strUrl.Substring(0, strUrl.IndexOf("?"))) + "?" + qs.ToString()); }
                }
            }
        }

        protected string HtmlSpace()
        {
            return "&nbsp; ";
        }

        protected bool CanAct(char typ, DataTable dtAuthRow, string id)
        {
            if (dtAuthRow != null)
            {
                if (typ == 'S')
                { // save, undo
                    DataRow dr = dtAuthRow.Rows[0];
                    return (Request.QueryString["enb"] != "N" &&
                            (dr["AllowAdd"].ToString() == "Y" && string.IsNullOrEmpty(id))
                            ||
                            (dr["AllowUpd"].ToString() == "Y" && !string.IsNullOrEmpty(id)));
                }
                else if (typ == 'N')
                { // new, copy
                    DataRow dr = dtAuthRow.Rows[0];
                    return (Request.QueryString["enb"] != "N" && dr["AllowAdd"].ToString() == "Y");
                }
                else if (typ == 'D')
                { // delete
                    DataRow dr = dtAuthRow.Rows[0];
                    return (Request.QueryString["enb"] != "N" && dr["AllowDel"].ToString() == "Y");
                }
            }
            return false;
        }

        protected void SwitchCmpPrj()
        {
            if (!string.IsNullOrEmpty(Request.QueryString["ssd"]))
            {
                System.Collections.Generic.Dictionary<string, string> CmpPrj = Session["CmpPrj" + Request.QueryString["ssd"]] as System.Collections.Generic.Dictionary<string, string>;
                if (CmpPrj != null)
                {
                    CmpPrj["cmp"] = LCurr.CompanyId.ToString();
                    CmpPrj["prj"] = LCurr.ProjectId.ToString();
                }
            }
        }

        protected void ImpersonateLogin()
        {
            if (!Request.IsLocal && Request.UserHostAddress != Request.ServerVariables["LOCAL_ADDR"]) throw new Exception("Access Denied");
            Dictionary<string, string> loginUsrInfo = Cache[Request.QueryString["runas"]] as Dictionary<string, string>;
            Cache.Remove(Request.QueryString["runas"]);
            LUser = new System.Web.Script.Serialization.JavaScriptSerializer().Deserialize<LoginUsr>(loginUsrInfo["LUser"]);
            LCurr = new System.Web.Script.Serialization.JavaScriptSerializer().Deserialize<UsrCurr>(loginUsrInfo["LCurr"]);
            LPref = new System.Web.Script.Serialization.JavaScriptSerializer().Deserialize<UsrPref>(loginUsrInfo["LPref"]);
            LImpr = new System.Web.Script.Serialization.JavaScriptSerializer().Deserialize<UsrImpr>(loginUsrInfo["LImpr"]);
            System.Web.Security.FormsAuthenticationTicket Ticket = new System.Web.Security.FormsAuthenticationTicket(LUser.LoginName, false, 3600);
            Context.User = new System.Security.Principal.GenericPrincipal(new System.Web.Security.FormsIdentity(Ticket), null);
            this.SystemsDict = (new LoginSystem()).GetSystemsList(string.Empty, string.Empty);
        }

        protected string PrepImpersonation()
        {
            string guid = Guid.NewGuid().ToString().Replace("-", "");
            string usr = new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(LUser);
            string curr = new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(LCurr);
            string impr = new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(LImpr);
            string pref = new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(LPref);

            Dictionary<string, string> loginInfo = new Dictionary<string, string>(){ 
                {"LUser",usr}
                ,{"LImpr",impr}
                ,{"LPref",pref}
                ,{"LCurr",curr}
            };
            Cache.Add(guid, loginInfo, null, DateTime.Now.AddSeconds(30), System.Web.Caching.Cache.NoSlidingExpiration, System.Web.Caching.CacheItemPriority.Default, null);
            return guid;
        }

        protected bool IsSelfInvoked()
        {
            try
            {
                return Request.IsLocal
                && !string.IsNullOrEmpty(Request.QueryString["runas"])
                && (Cache[Request.QueryString["runas"]] as Dictionary<string, string>) != null;
            }
            catch
            {
                return false;
            }
        }

        protected byte[] ReadToEnd(BinaryReader br, int length)
        {
            byte[] result;
            if (length > 0)
            {
                result = new byte[length];
                br.Read(result, 0, length);
                return result;
            }
            else
            {
                using (var ms = new MemoryStream())
                {
                    byte[] buffer = new byte[4096];
                    int count;
                    while ((count = br.Read(buffer, 0, buffer.Length)) != 0)
                        ms.Write(buffer, 0, count);
                    return ms.ToArray();
                }
            }

        }

        protected string CloneMyQueryString(List<string> ignore = null)
        {
            string[] qs = Request.QueryString.AllKeys.Where(key => ignore == null || !ignore.Contains(key, StringComparer.InvariantCultureIgnoreCase))
                                       .Select(key => key + "=" + HttpUtility.UrlEncode(Request.QueryString[key])).ToArray();
            return string.Join("&", qs.ToArray());
        }

        protected void VisitUrl(string url)
        {
            HttpWebRequest wr = (HttpWebRequest)HttpWebRequest.Create(url);
            wr.CookieContainer = new CookieContainer();
            wr.BeginGetResponse(x =>
            {
                try
                {
                    using (WebResponse resp = (x.AsyncState as WebRequest).EndGetResponse(x))
                    {
                        using (Stream stream = resp.GetResponseStream())
                        {
                            using (StreamReader sr = new StreamReader(stream))
                            {
                                sr.ReadToEnd();
                                sr.Close();
                            }
                            stream.Close();
                        }
                        resp.Close();
                    }
                }
                catch (WebException we)
                {
                    if (we.Status == WebExceptionStatus.ProtocolError)
                    {
                        var response = we.Response as HttpWebResponse;

                        if (response != null)
                        {
                            try
                            {
                                int status = (int)response.StatusCode;
                                if (status >= 400)
                                {
                                }
                                else if (status >= 300)
                                {
                                    throw new Exception("redirect " + url);
                                }
                            }
                            catch {
                                throw;
                            }
                        }
                        else
                        {
                            throw new Exception("no response code from web request " + url, we);
                            // no http status code available
                        }
                    }
                    else
                    {
                        throw new Exception("other error web request " + url, we);
                        // no http status code available
                    }
                }
                catch (Exception e) {
                    if (e == null) throw;
                }
            }, wr);
        }

        protected InvokeResult SelfInvoke(string url, string expectedContentType)
        {
            Uri myUri = Request.Url; 
            string ticket = PrepImpersonation();
            /* we cannot use myUrl.Host as dns name may not be the same as access from outside 
             * use either localhost assuming default web site or must be configured
             */
            string intDomainUrl = string.IsNullOrEmpty(Config.IntBaseUrl) 
                ? ((Request.IsSecureConnection ? "https://" : "http://")
                    + myUri.Host
                    + (myUri.IsDefaultPort ? "" : ":" + myUri.Port.ToString())
//                    + Request.ApplicationPath == "/" ? "" : Request.ApplicationPath
                    )
//                    ? "http://localhost/" + (Request.ApplicationPath + "/").Replace("//", "/") 
                    : GetDomainUrl(true);

            string newUrl =
                //myUri.Scheme
                //+ "://" + myUri.Host + ":" + myUri.Port
                intDomainUrl
                + ResolveUrl((url.StartsWith("~/") || url.StartsWith("/") || url.StartsWith("http") ? "" : "~/") + url) + (url.Contains("?") ? "&" : "?") + "runas=" + ticket;

            System.Net.HttpWebRequest request = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(newUrl);
            System.Net.HttpWebResponse response = (System.Net.HttpWebResponse)request.GetResponse();
            System.IO.BinaryReader br = new System.IO.BinaryReader(response.GetResponseStream());
            InvokeResult r = new InvokeResult();
            r.ContentType = response.ContentType;
            r.content = ReadToEnd(br, (int)response.ContentLength);
            try
            {
                string disposition = response.Headers["Content-Disposition"];
                int idx = disposition.ToLower().IndexOf("filename");
                string filename = disposition.Substring(idx + 8).Trim().TrimStart(new char[] { '=' }).Trim();
                r.ContentDisposition = filename;
            }
            catch { };
            return r;
        }

        protected void AnonymousLogin()
        {
            if (!Request.IsAuthenticated || this.LUser == null)
            {
                Credential cr = new Credential("Anonymous", "Anonymous");
                LoginUsr usr = (new LoginSystem()).GetLoginSecure(cr);
                if (usr != null && usr.UsrId > 0)
                {
                    LUser = usr;
                    (new LoginSystem()).SetLoginStatus("Anonymous", true, "", "", "");
                    System.Web.Security.FormsAuthenticationTicket Ticket = new System.Web.Security.FormsAuthenticationTicket("Anonymous", false, 3600);
                    System.Web.Security.FormsAuthentication.SetAuthCookie("Anonymous", false);
                    Context.User = new System.Security.Principal.GenericPrincipal(new System.Web.Security.FormsIdentity(Ticket), null);
                    byte csy = LUser.DefSystemId;
                    byte.TryParse(Request.QueryString["csy"], out csy);
                    this.LCurr = new UsrCurr(LUser.DefCompanyId, LUser.DefProjectId, csy, csy);
                    this.LImpr = null; this.SetImpersonation(this.LUser.UsrId);
                    this.LPref = (new LoginSystem()).GetUsrPref(this.LUser.UsrId, this.LCurr.CompanyId, this.LCurr.ProjectId, csy);
                    this.SystemsDict = (new LoginSystem()).GetSystemsList(string.Empty, string.Empty);
                    try { if (Session["Cache:currLang"] != null) LUser.CultureId = short.Parse(Session["Cache:currLang"].ToString()); }
                    catch { }
                }
            }
        }
        
        protected void CheckAuthentication(bool pageLoad, bool AuthRequired)
        {
            if (!Request.IsAuthenticated || this.LUser == null || (AuthRequired && LUser != null && LUser.LoginName.ToLower() == "anonymous"))
            {
                if (!AuthRequired)
                {
                    try
                    {
                        AnonymousLogin();
                    }
                    catch
                    {
                        if (!(
                            Request.IsLocal &&
                            Request.Url.GetComponents(UriComponents.Path, UriFormat.Unescaped).ToLower().Contains("encryptpwd.aspx") &&
                            (Request.QueryString["typ"] ?? "").ToUpper().Split(new char[] { ',' })[0] == "N")
                            )
                            throw;
                    };
                }
                else
                {
                    string loginUrl = (System.Web.Security.FormsAuthentication.LoginUrl ?? "").Replace("/" + Config.AppNameSpace + "/", "");
                    if (string.IsNullOrEmpty(loginUrl)) loginUrl = "MyAccount.aspx";
                    /* Get typ from Referrer.  UrlReferrer is null on Window.open.  */

                    string typ = (Request.QueryString["typ"] ?? "").ToUpper().Split(new char[] { ',' })[0];
                    bool isDefaultPage = new Regex("^/(\\w+/)*Default.aspx", RegexOptions.IgnoreCase).IsMatch(Request.Url.PathAndQuery.Split(new char[] { '?' })[0]);

                    if (string.IsNullOrEmpty(typ))
                    {
                        System.Text.RegularExpressions.Regex re = new System.Text.RegularExpressions.Regex("typ=[a-z0-9]+", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                        string referrer = Request.UrlReferrer != null ? Request.UrlReferrer.Query : "";
                        if (referrer.Contains("ReturnUrl"))
                        {
                            var returnUrlMatch = new System.Text.RegularExpressions.Regex("ReturnUrl=[^=]*").Match(referrer);
                            referrer = System.Web.HttpUtility.UrlDecode(returnUrlMatch.Value.Replace("ReturnUrl=", ""));
                        }
                        System.Text.RegularExpressions.Match match = re.Match(referrer);
                        if (match.Success) { typ = new System.Text.RegularExpressions.Regex("=[a-z0-9]+", System.Text.RegularExpressions.RegexOptions.IgnoreCase).Match(match.Value).Value.Substring(1); }
                    }
                    string rurl = "&ReturnUrl=" 
                                + Server.UrlEncode(
                                Request.Url.PathAndQuery 
                                + (!string.IsNullOrEmpty(typ) && !isDefaultPage ? (Request.QueryString["typ"] == null ? (Request.Url.PathAndQuery.IndexOf('?') > 0 ? "&" : "?") + "typ=" + typ : "") : ""));
                    this.Redirect(loginUrl + (loginUrl.IndexOf('?') > 0 ? "&" : "?") + "wrn=" + (pageLoad ? "1" : "2") + rurl);
                }
            }
            else
            {
                if (LUser.LoginName.ToLower() != "anonymous" &&
                    ((!LUser.OTPValidated && LUser.TwoFactorAuth && Config.EnableTwoFactorAuth == "Y")// block if second factor is not verified
                    ||
                    (string.IsNullOrEmpty(LUser.Provider) && (LUser.PwdChgDt == null || (LUser.PwdDuration == 0 ? false : (DateTime.Today > LUser.PwdChgDt.Value.AddDays(LUser.PwdDuration)))))
                    ))
                {
                    string loginUrl = (System.Web.Security.FormsAuthentication.LoginUrl ?? "").Replace("/" + Config.AppNameSpace + "/", "");
                    if (string.IsNullOrEmpty(loginUrl)) loginUrl = "MyAccount.aspx";
                    /* Get typ from Referrer.  UrlReferrer is null on Window.open.  */

                    string typ = (Request.QueryString["typ"] ?? "").ToUpper().Split(new char[] { ',' })[0];
                    bool isDefaultPage = new Regex("^/(\\w+/)*Default.aspx", RegexOptions.IgnoreCase).IsMatch(Request.Url.PathAndQuery.Split(new char[] { '?' })[0]);

                    if (string.IsNullOrEmpty(typ))
                    {
                        System.Text.RegularExpressions.Regex re = new System.Text.RegularExpressions.Regex("typ=[a-z0-9]+", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                        System.Text.RegularExpressions.Match match = re.Match(Request.UrlReferrer != null ? Request.UrlReferrer.Query : "");
                        if (match.Success) { typ = new System.Text.RegularExpressions.Regex("=[a-z0-9]+", System.Text.RegularExpressions.RegexOptions.IgnoreCase).Match(match.Value).Value.Substring(1); }
                    }
                    string rurl = "&ReturnUrl=" + Server.UrlEncode(Request.Url.PathAndQuery + (!string.IsNullOrEmpty(typ) && !isDefaultPage ? (Request.QueryString["typ"] == null ? (Request.Url.PathAndQuery.IndexOf('?') > 0 ? "&" : "?") + "typ=" + typ : "") : ""));
                    this.Redirect(loginUrl + (loginUrl.IndexOf('?') > 0 ? "&" : "?") + "wrn=3" + rurl);
                }
            }
        }
        protected void EnforceSSL()
        {
            if (!Request.IsLocal 
                && (Config.EnableSsl ||
                    (IsProxy() && Config.ExtBaseUrl.ToLower().StartsWith("https:"))) 
                && !Request.Path.ToLower().Contains("msg.aspx"))
            {
                System.Web.Configuration.SessionStateSection SessionSettings = ConfigurationManager.GetSection("system.web/sessionState") as System.Web.Configuration.SessionStateSection;
                string sessionCookieName = SessionSettings != null ? SessionSettings.CookieName : null;

                HttpCookie sessionCookie = Response.Cookies[sessionCookieName];
                if (Request.Cookies["secureChannel"] == null)
                {
                    HttpCookie x = new HttpCookie("secureChannel", "test");
                    x.Secure = true;
                    x.HttpOnly = true;
                    Response.AppendCookie(x);
                    HttpCookie y = new HttpCookie("secureChannelResult", "test");
                    y.Secure = true;
                    y.HttpOnly = true;
                    Response.AppendCookie(y);
                    if (
                        !string.IsNullOrEmpty(Request.Headers["X-ARR-LOG-ID"])
                        && 
                        (
                        Request.Headers["X-Forwarded-Https"] != "on" // iis urlrewrite can't handle same domain redirect from http => https, endless loop
                        //Request.Headers["Front-End-Https"] == "off"
                        )
                        )
                    {
                        ErrorTrace(new Exception(string.Format("Proxy configuration issue for IIS UrlRewrite(https:{0}) vs {1}, must enforce https:// before proxying", Request.Headers["X-Forwarded-Https"] ?? "null", Config.ExtBaseUrl))
                        , "warning", null, Request);

                        throw new Exception(string.Format("Please use <a href='{0}'> {0} </a>", ResolveUrlCustom(Request.Url.AbsoluteUri, false, true)));
                    }
                    else
                    {
                        if (sessionCookie != null)
                        {
                            Response.Cookies[sessionCookieName].Expires = new DateTime(1900, 1, 1);
                        }
                        this.Redirect(
                        //    ResolveUrlCustom(Request.Url.AbsoluteUri, false, true)
                            Request.Url.AbsoluteUri.Replace("http://", "https://")
                        );
                    }
                }
                if (sessionCookie != null)
                {
                    if (Request.Cookies[sessionCookieName] != null
                        && Request.Cookies[sessionCookieName].Value != sessionCookie.Value)
                    {
                        Response.Cookies.Remove(sessionCookieName);
                        Response.Cookies.Add(Request.Cookies[sessionCookieName]);
                        Request.Cookies[sessionCookieName].Secure = true;
                        Request.Cookies[sessionCookieName].HttpOnly = true;
                    }
                    else
                    {
                        sessionCookie.Secure = true;
                        sessionCookie.HttpOnly = true;
                    }
                }
            }
        }
        /* To get around a problem on IE */
        //protected string GetUriKey(string key, bool first)
        //{
        //    string rtn = string.Empty;
        //    string myHost = Request.Url.Host;
        //    string referrerHost = Request.UrlReferrer != null ? Request.UrlReferrer.Host : "";
        //    if (myHost == referrerHost)
        //    {
        //        /* Get ssd from last url for the case only for hyperlink/imagelink.  UrlReferrer is null on Window.open.  */
        //        System.Text.RegularExpressions.Regex re = new System.Text.RegularExpressions.Regex(key + "=[0-9]+", RegexOptions.IgnoreCase);
        //        foreach (Match match in re.Matches(Request.UrlReferrer != null ? Request.UrlReferrer.Query : ""))
        //        {
        //            if (match.Success) { rtn = new System.Text.RegularExpressions.Regex("[0-9]+").Match(match.Value).Value; if (first) return rtn; }
        //        }
        //    }
        //    return rtn;
        //}

        protected string SenderFocusId(object sender)
        {
            if (sender is RoboCoder.WebControls.ComboBox) return ((RoboCoder.WebControls.ComboBox)sender).FocusID;
            else return ((WebControl)sender).ClientID;
        }
        protected void TranslateItem(Label lb, DataRowCollection rows, string key)
        {
            try
            {
                lb.Text = rows.Find(key)[1].ToString();
            }
            catch { lb.Text = "ERR!"; }
        }
        protected string TranslateItem(DataRowCollection rows, string key)
        {
            try
            {
                return rows.Find(key)[1].ToString();
            }
            catch { return "ERR!"; }
        }
        protected void TranslateItem(Button btn, DataRowCollection rows, string key)
        {
            try
            {
                btn.Text = rows.Find(key)[1].ToString();
            }
            catch { btn.Text = "ERR!"; }
        }
        protected void TranslateItem(Button btn, DataRowCollection rows, string key, object[] param)
        {
            try
            {
                btn.Text = string.Format(rows.Find(key)[1].ToString(), param);
            }
            catch { btn.Text = "ERR!"; }
        }

        protected void TranslateItem(CheckBox cb, DataRowCollection rows, string key)
        {
            try
            {
                cb.Text = rows.Find(key)[1].ToString();
            }
            catch { cb.Text = "ERR!"; }
        }

        protected void TranslateItem(LinkButton btn, DataRowCollection rows, string key)
        {
            try
            {
                btn.Text = rows.Find(key)[1].ToString();
            }
            catch { btn.Text = "ERR!"; }
        }

        protected void TranslateItem(HyperLink hl, DataRowCollection rows, string key)
        {
            try
            {
                hl.Text = rows.Find(key)[1].ToString();
            }
            catch { hl.Text = "ERR!"; }
        }

        protected Int16 GetCurrCultureId()
        {
            const string KEY_browserLang = "Cache:browserLang";
            if (LUser == null) { return 1; }      // It should not even get here.
            else
            {
                if ((LUser.LoginName ?? "").ToLower() == "anonymous" && Session[KEY_browserLang] == null)   // First time Anonymous.
                {
                    string lang = Request.UserLanguages != null && Request.UserLanguages.Length > 0 ? Request.UserLanguages[0] : "en";
                    Int16 cid = (new AdminSystem()).GetCult(lang);      // Return 1 (en-US) when not found;
                    Session[KEY_browserLang] = cid.ToString();
                    LUser.CultureId = cid;
                }
                return LUser.CultureId;
            }
        }

        protected string SanitizeInput(string inStr)
        {
            Regex[] aRe = { new Regex(@"<(\s|/)*script[^>]*>"), new Regex(@"<(\s|/)*style[^>]*>") };
            foreach (var re in aRe)
            {
                inStr = re.Replace(inStr, "");
            }
            return inStr;
        }

        //protected string SetLoc()
        //{
        //    TimeZoneInfo tzInfo = Session["Cache:tzInfo"] as TimeZoneInfo ?? TimeZoneInfo.Local;
        //    bool isDST = int.Parse(Session["Cache:tzDST"] as string ?? "0") == 1;
        //    DateTime nowUTC = DateTime.Now.ToUniversalTime();
        //    DateTime nowTarget = TimeZoneInfo.ConvertTimeFromUtc(nowUTC, tzInfo);
        //    TimeSpan tzOffset = nowUTC.Subtract(nowTarget);
        //    int offset = (0 - tzOffset.Hours * 60 + tzOffset.Minutes) / 60;

        //    return (Session["Cache:City"] ?? "").ToString() + " " + (Session["Cache:State"] ?? "").ToString() + " " + (isDST ? tzInfo.DaylightName : tzInfo.StandardName) + " GMT" + (offset < 0 ? "" : "+") + offset.ToString(); ;
        //}

        protected string TimeZoneOffset()
        {
            TimeZoneInfo tzInfo = Session["Cache:tzInfo"] as TimeZoneInfo ?? TimeZoneInfo.Local;
            bool isDST = int.Parse(Session["Cache:tzDST"] as string ?? "0") == 1;
            DateTime nowUTC = DateTime.Now.ToUniversalTime();
            DateTime nowTarget = TimeZoneInfo.ConvertTimeFromUtc(nowUTC, tzInfo);
            TimeSpan tzOffset = nowUTC.Subtract(nowTarget);
            return (tzOffset.Hours * 60 + tzOffset.Minutes).ToString();
        }

        protected TimeZoneInfo CurrTimeZoneInfo()
        {
            return Session["Cache:tzInfo"] as TimeZoneInfo ?? TimeZoneInfo.Local;
        }

        protected string ToIntDateTime(string datetime)
        {
            if (string.IsNullOrEmpty(datetime)) return "";
            else return DateTime.Parse(datetime, System.Threading.Thread.CurrentThread.CurrentCulture).ToString("F");
        }

        protected string ToIntDateTime(string datetime, bool isUTC, bool forceConvert)
        {
            if (string.IsNullOrEmpty(datetime)) return "";
            else
            {
                var d = DateTime.Parse(datetime, System.Threading.Thread.CurrentThread.CurrentCulture).ToString("F");
                if (isUTC) return SetDateTimeUTC(d, forceConvert);
                else return d;
            }
        }

        protected string SetDateTimeUTC(string datetime, bool forceConvert)
        {
            DateTime d = Convert.ToDateTime(datetime, System.Threading.Thread.CurrentThread.CurrentCulture);
            if (d.Hour == 0 && d.Minute == 0 && d.Second == 0 && d.Millisecond == 0 && !forceConvert) return datetime;
            TimeZoneInfo tzinfo = Session["Cache:tzInfo"] as TimeZoneInfo ?? TimeZoneInfo.Local;
            return TimeZoneInfo.ConvertTimeToUtc(d, tzinfo).ToString();
        }
        protected DateTime SetDateTimzeLocal(string datetimeUTC, bool forceConvert)
        {
            DateTime d = Convert.ToDateTime(datetimeUTC, System.Threading.Thread.CurrentThread.CurrentCulture);
            if (d.Hour == 0 && d.Minute == 0 && d.Second == 0 && d.Millisecond == 0 && !forceConvert) return d;
            TimeZoneInfo tzinfo = Session["Cache:tzInfo"] as TimeZoneInfo ?? TimeZoneInfo.Local;
            return TimeZoneInfo.ConvertTimeFromUtc(d, tzinfo);

        }
        protected void CovertRptUTC(DataTable dt)
        {
            if (dt == null) return;

            List<int> ord = new List<int>();

            if (dt.Columns.Contains("ModifiedOn")) ord.Add(dt.Columns["ModifiedOn"].Ordinal);
            if (dt.Columns.Contains("InputOn")) ord.Add(dt.Columns["InputOn"].Ordinal);
            if (dt.Columns.Contains("UsageDt")) ord.Add(dt.Columns["UsageDt"].Ordinal);
            if (ord.Count > 0)
            {
                foreach (DataRow dr in dt.Rows)
                {
                    foreach (int o in ord)
                    {
                        dr[o] = SetDateTimzeLocal(dr[o].ToString(), false);
                    }
                }
            }
        }

        /* To overcome client rule not recognized in a multiple tabs situation */
        public override Control FindControl(string id)
        {
            Control cc = base.FindControl(id);
            if (cc != null || findControlId == id) return cc;

            AjaxControlToolkit.TabContainer tc = base.FindControl("cTabContainer") as AjaxControlToolkit.TabContainer;
            if (tc != null)
            {
                foreach (AjaxControlToolkit.TabPanel tab in tc.Tabs)
                {
                    findControlId = id;
                    cc = tab.FindControl(id);
                    findControlId = null;
                    if (cc != null) return cc;
                }
            }
            return cc;
        }

        // Procedure for foreign currency rate from external source (do not call this repeatedly, use timer to break up the calls):
        public string GetExtFxRate(string FrISOCurrencySymbol, string ToISOCurrencySymbol)
        {
            try
            {
                /* The following iGoogle Currency Converter has been retired by Google */
                /*
                System.Web.Script.Serialization.JavaScriptSerializer jss = new System.Web.Script.Serialization.JavaScriptSerializer();
                System.Text.RegularExpressions.Regex re = new Regex("^[0-9.]+\\s");
                string url = string.Format("http://www.google.com/ig/calculator?hl={0}&q=1{1}=?{2}", "en", FrISOCurrencySymbol, ToISOCurrencySymbol);
                System.Net.HttpWebRequest request = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(url);
                request.Referer = "http://www.checkmin.com";
                System.Net.HttpWebResponse response = (System.Net.HttpWebResponse)request.GetResponse();
                System.IO.StreamReader sr = new System.IO.StreamReader(response.GetResponseStream());
                string result = sr.ReadToEnd();
                System.Collections.Generic.Dictionary<string, string> o = jss.Deserialize<System.Collections.Generic.Dictionary<string, string>>(result);
                if (o["error"] == "0" || o["error"] == "")
                {
                    Match m = re.Match(o["rhs"]);
                    if (m.Success) { return m.Value.Trim(); }
                }
                */
                /* new google page crawling */
                //System.Web.Script.Serialization.JavaScriptSerializer jss = new System.Web.Script.Serialization.JavaScriptSerializer();
                //System.Text.RegularExpressions.Regex re = new Regex("^[0-9.]+\\s");
                //string url = String.Format("https://www.google.com/finance/converter?a={0}&from={1}&to={2}&meta={3}", "1", FrISOCurrencySymbol, ToISOCurrencySymbol, Guid.NewGuid().ToString());
                //System.Net.HttpWebRequest request = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(url);
                //request.Referer = "http://www.checkmin.com";
                //System.Net.HttpWebResponse response = (System.Net.HttpWebResponse)request.GetResponse();
                //System.IO.StreamReader sr = new System.IO.StreamReader(response.GetResponseStream());
                //var rate = Regex.Matches(sr.ReadToEnd(),"<span class=\"?bld\"?>([0-9.]+)(.*)</span>")[0].Groups[1].Value;
                //return rate.Trim().Replace(",", ".");
                var URL = new UriBuilder("https://pro-api.coinmarketcap.com/v1/tools/price-conversion");
                var CmcAPIKey = System.Configuration.ConfigurationManager.AppSettings["CMCAPIKey"];

                var queryString = HttpUtility.ParseQueryString(string.Empty);
                queryString["amount"] = "1";
                queryString["symbol"] = FrISOCurrencySymbol;
                queryString["convert"] = ToISOCurrencySymbol;

                URL.Query = queryString.ToString();

                var client = new WebClient();
                client.Headers.Add("X-CMC_PRO_API_KEY", CmcAPIKey);
                client.Headers.Add("Accepts", "application/json");
                string jsonString = client.DownloadString(URL.ToString());
                var price = Newtonsoft.Json.Linq.JObject.Parse(jsonString).SelectToken("['data'].['quote'].['" + ToISOCurrencySymbol + "']['price']");
                return price.ToString();
            }
            // Cannot add "ex.Message" to the return statement; do not remove "ex"; need it here for debugging purpose.
            catch (Exception ex)
            {
                if (ex != null) return string.Empty;
                else return string.Empty;
            }
        }

        public string fxEncryptedText(string ss)
        {
            return ss;
            /* for the time being:
            int i = ss.IndexOf('-');
            if (i >= 0) return new string('X', i) + ss.Substring(i, ss.Length - i);
            else return new string('X', ss.Length);
            */
        }

        public string fxMoney(string ss, string FrCulture, string ToCulture)
        {
            if (ss.Equals(string.Empty) || string.IsNullOrEmpty(FrCulture) || string.IsNullOrEmpty(ToCulture)) { return ss; }
            else
            {
                System.Globalization.CultureInfo FxInfo = new System.Globalization.CultureInfo("en-US");
                System.Globalization.CultureInfo LuInfo = new System.Globalization.CultureInfo(LUser.Culture);
                System.Globalization.CultureInfo FrInfo = new System.Globalization.CultureInfo(FrCulture);
                System.Globalization.CultureInfo ToInfo = new System.Globalization.CultureInfo(ToCulture);
                string FrCurrency = (new System.Globalization.RegionInfo(FrInfo.LCID)).ISOCurrencySymbol;
                string ToCurrency = (new System.Globalization.RegionInfo(ToInfo.LCID)).ISOCurrencySymbol;
                if (FrCurrency != ToCurrency)
                {
                    /* Use client-side cache to make this faster only if GetFxRate proves to be slow and being called many times within a day */
                    DataTable dt = (new AdminSystem()).GetFxRate(FrCurrency, ToCurrency);   // Need the presence of FxRate table in ??Cmon database.
                    if (string.IsNullOrEmpty(dt.Rows[0]["ToFxRate"].ToString()) || (DateTime)dt.Rows[0]["ValidFr"] != DateTime.Now.ToUniversalTime().Date)
                    {
                        // FxRate table found but Fx rate not available for today.
                        string ToFxRate = GetExtFxRate(FrCurrency, ToCurrency);     // Get current rate if available.
                        if (!string.IsNullOrEmpty(ToFxRate))
                        {
                            // Use latest rate.
                            ss = (Decimal.Parse(ss, LuInfo) * Decimal.Parse(ToFxRate, FxInfo)).ToString();
                            (new AdminSystem()).UpdFxRate(FrCurrency, ToCurrency, ToFxRate);
                        }
                        else if (!string.IsNullOrEmpty(dt.Rows[0]["ToFxRate"].ToString()))
                        {
                            // Old rate is better than no rate.
                            ss = (Decimal.Parse(ss, LuInfo) * Decimal.Parse(dt.Rows[0]["ToFxRate"].ToString(), LuInfo)).ToString();
                        }
                    }
                    else // Rate table is up-to-date.
                    {
                        ss = (Decimal.Parse(ss, LuInfo) * Decimal.Parse(dt.Rows[0]["ToFxRate"].ToString(), LuInfo)).ToString();
                    }
                }
                return RO.Common3.Utils.fmMoney(ss, ToCulture);
            }
        }

        public string fxCurrency(string ss, string FrCulture, string ToCulture)
        {
            return RO.Common3.Utils.fmCurrency(fxMoney(ss, FrCulture, ToCulture), ToCulture);
        }

        // Overload to handle customized SMTP configuration.
        public Int32 SendEmail(string subject, string body, string to, string from, string replyTo, string fromTitle, bool isHtml, string smtp)
        {
            return SendEmail(subject, body, to, from, replyTo, fromTitle, isHtml, new List<System.Net.Mail.Attachment>(), smtp);
        }

        // "to" may contain email addresses separated by ";".
        public Int32 SendEmail(string subject, string body, string to, string from, string replyTo, string fromTitle, bool isHtml)
        {
            return SendEmail(subject, body, to, from, replyTo, fromTitle, isHtml, new List<KeyValuePair<string, byte[]>> { });
        }

        // Overload to handle attachments and being called by the above.
        public Int32 SendEmail(string subject, string body, string to, string from, string replyTo, string fromTitle, bool isHtml, List<KeyValuePair<string, byte[]>> att)
        {
            List<System.Net.Mail.Attachment> mailAtts = new List<System.Net.Mail.Attachment>();
            foreach (var f in att)
            {
                var ms = new MemoryStream(f.Value);
                mailAtts.Add(new System.Net.Mail.Attachment(ms, f.Key));
            }
            return SendEmail(subject, body, to, from, replyTo, fromTitle, isHtml, mailAtts, string.Empty);
        }

        // Overload to handle attachments and being called by the above and should not be called publicly.
        // Return number of emails sent today; users should not exceed 10,000 a day in order to avoid smtp IP labelled as spam email.
        public Int32 SendEmail(string subject, string body, string to, string from, string replyTo, string fromTitle, bool isHtml, List<System.Net.Mail.Attachment> att, string smtp)
        {
            Int32 iEmailsSentToday = (new RO.WebRules.WebRule()).CountEmailsSent();
            string[] smtpConfig = (string.IsNullOrEmpty(smtp) ? Config.SmtpServer : smtp).Split(new char[] { '|' });
            bool bSsl = smtpConfig[0].Trim() == "true" ? true : false;
            int port = smtpConfig.Length > 1 ? int.Parse(smtpConfig[1].Trim()) : 25;
            string server = smtpConfig.Length > 2 ? smtpConfig[2].Trim() : null;
            string username = smtpConfig.Length > 3 ? smtpConfig[3].Trim() : null;
            string password = smtpConfig.Length > 4 ? smtpConfig[4].Trim() : null;
            string domain = smtpConfig.Length > 5 ? smtpConfig[5].Trim() : null;
            System.Net.Mail.MailMessage mm = new System.Net.Mail.MailMessage();
            string[] receipients = to.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var t in receipients)
            {
                mm.To.Add(new System.Net.Mail.MailAddress(t.Trim()));
            }
            mm.Subject = subject;
            mm.Body = body;
            if (att != null && att.Count > 0)
            {
                foreach (var item in att) { mm.Attachments.Add(item); }
            }
            mm.IsBodyHtml = isHtml;
            mm.From = new System.Net.Mail.MailAddress(string.IsNullOrEmpty(username) || !(username ?? "").Contains("@") ? from : username, string.IsNullOrEmpty(fromTitle) ? from : fromTitle);    // Address must be the same as the smtp login user.
            mm.ReplyToList.Add(new System.Net.Mail.MailAddress(string.IsNullOrEmpty(replyTo) ? from : replyTo)); // supplied from would become reply too for the 'sending on behalf of'
            (new RO.WebRules.WebRule()).SendEmail(bSsl, port, server, username, password, domain, mm);
            mm.Dispose();   // Error is trapped and reported from the caller.
            return iEmailsSentToday;
        }

        #region Document zip download helpers
        // must be in-sync with AsmxBase.cs
        protected string EncodeRequest<T>(T request, bool noEncrypt = false)
        {
            int round = 1;
            var jss = new System.Web.Script.Serialization.JavaScriptSerializer();
            string requestJSON = jss.Serialize(request);
            if (noEncrypt) return base64UrlEncode(System.Text.UTF8Encoding.UTF8.GetBytes(requestJSON));
            RO.Facade3.Auth authObject = GetAuthObject();
            string password = authObject.GetSessionEncryptionKey("", ""); // global non-user specific
            string salt = password; // global, no salt
            string encodedRequest = base64UrlEncode(
                                        Encrypt(System.Text.UTF8Encoding.UTF8.GetBytes(requestJSON),
                                                System.Text.UTF8Encoding.UTF8.GetBytes(password),
                                                System.Text.UTF8Encoding.UTF8.GetBytes(salt), round));
            return encodedRequest;
        }
        protected T DecodeRequest<T>(string encodedRequest, bool noEncrypt = false)
        {
            int round = 1;
            byte[] x = base64UrlDecode(encodedRequest);
            var jss = new System.Web.Script.Serialization.JavaScriptSerializer();
            RO.Facade3.Auth authObject = GetAuthObject();
            string password = authObject.GetSessionEncryptionKey("", ""); // global non-user specific
            string salt = password; // global, no salt
            T request = jss.Deserialize<T>(
                                            System.Text.UTF8Encoding.UTF8.GetString(
                                            noEncrypt
                                            ? x
                                            : Decrypt(x,
                                                System.Text.UTF8Encoding.UTF8.GetBytes(password),
                                                System.Text.UTF8Encoding.UTF8.GetBytes(salt), round)));
            return request;
        }

        protected string EncodeZipDownloadRequest(ZipDownloadRequest request, bool noEncrypt = false)
        {
            int round = 1;
            var jss = new System.Web.Script.Serialization.JavaScriptSerializer();
            string requestJSON = jss.Serialize(request);
            if (noEncrypt) return base64UrlEncode(System.Text.UTF8Encoding.UTF8.GetBytes(requestJSON));
            RO.Facade3.Auth authObject = GetAuthObject();
            string password = authObject.GetSessionEncryptionKey("", ""); // global non-user specific
            string salt = password; // global, no salt
            string encodedRequest = base64UrlEncode(
                                        Encrypt(System.Text.UTF8Encoding.UTF8.GetBytes(requestJSON),
                                                System.Text.UTF8Encoding.UTF8.GetBytes(password),
                                                System.Text.UTF8Encoding.UTF8.GetBytes(salt), round));
            return encodedRequest;
        }
        protected ZipDownloadRequest DecodeZipDownloadRequest(string encodedRequest, bool noEncrypt = false)
        {
            // must be in-sync with AsmxBase.cs
            int round = 1;
            byte[] x = base64UrlDecode(encodedRequest);
            var jss = new System.Web.Script.Serialization.JavaScriptSerializer();
            RO.Facade3.Auth authObject = GetAuthObject();
            string password = authObject.GetSessionEncryptionKey("", ""); // global non-user specific
            string salt = password; // global, no salt
            ZipDownloadRequest request = jss.Deserialize<ZipDownloadRequest>(
                                            System.Text.UTF8Encoding.UTF8.GetString(
                                            noEncrypt
                                            ? x
                                            : Decrypt(x,
                                                System.Text.UTF8Encoding.UTF8.GetBytes(password),
                                                System.Text.UTF8Encoding.UTF8.GetBytes(salt), round)));

            return request;
        }

        protected byte[] GetColumnContent(string systemId, string screenId, string mstId, string tableName, string keyColumnName, string columnName)
        {
            byte sid = byte.Parse(systemId);
            int scrId = int.Parse(screenId);
            string dbConnectionString = AppConnectStr(sid);
            string dbPwd = AppPwd(sid);
            DataTable dt = (new AdminSystem()).GetDbImg(mstId, tableName, keyColumnName, columnName, dbConnectionString, dbPwd);
            return ((byte[])dt.Rows[0][0]);
        }

        protected virtual Ionic.Zip.ZipFile GetMultiDoc(string systemId, string screenId, string mstId, string spName, string tableName, string parentDirectory, Ionic.Zip.ZipFile zipObject, string rootDirectory, string docLs = null)
        {
            byte sid = byte.Parse(systemId);
            int scrId = int.Parse(screenId);
            string dbConnectionString = AppConnectStr(sid);
            string dbPwd = AppPwd(sid);
            List<string> selectedIds = docLs == null ? null : docLs.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToList();
            DataTable dt = (new AdminSystem()).GetDdl(scrId, spName, false, false, 0, mstId, dbConnectionString, dbPwd, string.Empty, LImpr, LCurr);
            if (dt.Rows.Count > 0)
            {
                string baseDirectory = rootDirectory + "/" + parentDirectory;

                foreach (DataRow dr in dt.Rows)
                {
                    if (!Directory.Exists(baseDirectory)) Directory.CreateDirectory(baseDirectory);
                    string docId = dr["DocId"].ToString();
                    if (docLs != null && !docLs.Contains(docId)) continue;
                    DataTable dtDoc = (new AdminSystem()).GetDbDoc(docId, tableName, dbConnectionString, dbPwd);
                    if (dtDoc.Rows.Count > 0)
                    {
                        string tempLocation = baseDirectory + "/" + dr["DocName"].ToString();
                        byte[] content = dtDoc.Rows[0]["DocImage"] as byte[];
                        using (FileStream fs = new FileStream(tempLocation, FileMode.Create))
                        {
                            fs.Write(content, 0, content.Length);
                            fs.Close();
                        }
                        File.SetCreationTimeUtc(tempLocation, (DateTime)dr["InputOn"]);
                        File.SetLastWriteTimeUtc(tempLocation, (DateTime)dr["InputOn"]);
                    }

                }
                zipObject.AddDirectory(baseDirectory, parentDirectory);
            }
            return zipObject;
        }
        protected virtual byte[] GetMultiDoc(RO.Web.ZipDownloadRequest request)
        {
            string tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            try
            {
                Directory.CreateDirectory(tempDirectory);
                using (MemoryStream ms = new MemoryStream())
                {
                    using (Ionic.Zip.ZipFile resultFile = new Ionic.Zip.ZipFile())
                    {
                        resultFile.CompressionMethod = Ionic.Zip.CompressionMethod.Deflate;
                        resultFile.CompressionLevel = Ionic.Zlib.CompressionLevel.BestCompression;
                        foreach (var r in request.md)
                        {
                            int usrId = int.Parse(r.scr[3]);
                            LImpr = null;
                            SetImpersonation(usrId);
                            foreach (var c in r.cols)
                            {
                                try
                                {
                                    GetMultiDoc(r.scr[0], r.scr[1], r.scr[2], c[0], c[1], c[2], resultFile, tempDirectory);
                                }
                                catch (Exception ex)
                                {
                                    ErrorTrace(new Exception(string.Format("systemId {0}", string.Join(",", r.scr.ToArray())), ex), "error", null, Request);
                                    throw;
                                }
                            }
                        }

                        resultFile.Save(ms);
                    }
                    ms.Close();
                    return ms.ToArray();
                };

            }
            catch (Exception ex)
            {
                Exception e = new Exception("problem zipping documents", ex);
                ErrorTrace(e, "error", null, Request);
                throw;
            }
            finally
            {
                try
                {
                    Directory.Delete(tempDirectory, true);
                }
                catch (Exception ex)
                {
                    ErrorTrace(new Exception("problem removing temp directory for zip usage", ex), "warning", null, Request);
                }
            }

        }
        protected virtual RO.Web.ZipMultiDocRequest MakeZipMultiDocRequest(byte sysId, int screenId, string mstId, string getListSPName, string docTableName, string baseDirectoryName, List<string> docIdLs = null)
        {
            return new RO.Web.ZipMultiDocRequest()
            {
                scr = new List<string>() { sysId.ToString(), screenId.ToString(), mstId, LUser.UsrId.ToString() },
                cols = new List<List<string>>() { new List<string>() { getListSPName, docTableName, baseDirectoryName, docIdLs == null ? null : string.Join(",", docIdLs.ToArray()) } }
            };
        }
        protected virtual List<RO.Web.ZipMultiDocRequest> CompressZipMultiDocRequest(List<RO.Web.ZipMultiDocRequest> md)
        {
            List<RO.Web.ZipMultiDocRequest> _md = new List<RO.Web.ZipMultiDocRequest>();
            Func<List<List<string>>, List<List<string>>> shortenContent = cols => cols.Select(r => (r.Select((c, i) => i == 0 ? c.Replace("GetDdl", "") : c)).ToList()).ToList();
            foreach (var _x in md)
            {
                bool merged = false;
                foreach (var y in _md)
                {
                    if (Enumerable.SequenceEqual(_x.scr, y.scr))
                    {

                        //                        y.cols.AddRange(_x.cols.Select(r => (r.Select((c, i) => i == 0 ? c.Replace("GetDdl", "") : c)).ToList()));
                        y.cols.AddRange(shortenContent(_x.cols));
                        merged = true;
                    }
                }
                if (!merged) _md.Add(new RO.Web.ZipMultiDocRequest() { scr = _x.scr, cols = shortenContent(_x.cols) });
            }
            return _md;
        }
        protected virtual List<RO.Web.ZipMultiDocRequest> ExpandZipMultiDocRequest(List<RO.Web.ZipMultiDocRequest> md)
        {
            return md.Select(z => new RO.Web.ZipMultiDocRequest()
            {
                scr = z.scr
                ,
                cols = z.cols.Select(r => r.Select((c, i) => i == 0 ? "GetDdl" + c : c).ToList()).ToList()
            }).ToList();
        }
        protected virtual void ReturnAsAttachment(byte[] content, string fileName, string mimeType = "application/octet-stream", bool inline = true)
        {
            Response.Buffer = true;
            Response.ClearHeaders();
            Response.ClearContent();
            Response.ContentType = mimeType;
            string contentDisposition = inline ? "attachment" : "inline";
            Response.AppendHeader("Content-Disposition", contentDisposition + "; Filename=" + fileName);
            Response.BinaryWrite(content);
            Response.End();
        }

        #endregion
        protected void LoadGoogleClient(string clientId)
        {
            ScriptManager.RegisterStartupScript(this, this.GetType(), "GoogleClient",
                "<script type='text/javascript'>"
                + "var googleClientId = '" + clientId + "';"
                + "</script>"
                + "<script type='text/javascript' src='https://apis.google.com/js/client.js'></script>"
                , false);
        }
        protected void LoadFacebookClient(string appId, string lang, string channelUrl)
        {
            ScriptManager.RegisterStartupScript(this, this.GetType(), "FacebookClient",
                "<script type='text/javascript'>"
                + " window.fbAsyncInit = function () {FB.init({appId: '" + appId + "',channelUrl: '" + channelUrl + "',status: false,cookie: true,xfbml: true,oauth: true});};"
                + "var facebookAppId = '" + appId + "';"
                + "</script>"
                + "<script type='text/javascript' src='https://connect.facebook.net/" + (string.IsNullOrEmpty(lang) ? "en-us": lang) + "/all.js'></script>"
                , false);
        }

        protected Dictionary<string, object> GetGoogleProfile(string accessToken)
        {
            string gooleProfileUrl = "https://www.googleapis.com/oauth2/v1/userinfo?access_token=" + System.Web.HttpUtility.UrlEncode(accessToken);
            Dictionary<string, object> profile = new System.Collections.Generic.Dictionary<string, object>();
            try
            {
                System.Net.HttpWebRequest request = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(new Uri(gooleProfileUrl));
                System.Net.HttpWebResponse response = (System.Net.HttpWebResponse)request.GetResponse();
                System.IO.StreamReader readStream = new System.IO.StreamReader(response.GetResponseStream(), System.Text.Encoding.UTF8);
                string rtn = readStream.ReadToEnd();
                // google API returns JSONEncode(htmlencoded(string)) and we need to reverse it
                profile = new System.Web.Script.Serialization.JavaScriptSerializer().Deserialize<System.Collections.Generic.Dictionary<string, object>>(rtn);
                return profile;
            }
            catch { return profile; }
        }

        protected Dictionary<string, object> GetFacebookProfile(string accessToken)
        {
            string facebookProfileUrl = "https://graph.facebook.com/me?fields=id,name,email&access_token=" + System.Web.HttpUtility.UrlEncode(accessToken);
            Dictionary<string, object> profile = new System.Collections.Generic.Dictionary<string, object>();
            try
            {
                System.Net.HttpWebRequest request = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(new Uri(facebookProfileUrl));
                System.Net.HttpWebResponse response = (System.Net.HttpWebResponse)request.GetResponse();
                System.IO.StreamReader readStream = new System.IO.StreamReader(response.GetResponseStream(), System.Text.Encoding.UTF8);
                string rtn = readStream.ReadToEnd();
                // facebook API returns JSONEncode(htmlencoded(string)) and we need to reverse it
                profile = new System.Web.Script.Serialization.JavaScriptSerializer().Deserialize<System.Collections.Generic.Dictionary<string, object>>(rtn);
                return profile;
            }
            catch { return profile; }
        }

        protected Dictionary<string, Dictionary<string, X509Certificate2>> GetMicrosoftOnLineSigners()
        {
            if (Cache["MicrosoftOnlineSigner"] as Dictionary<string, Dictionary<string, X509Certificate>> == null)
            {
                /* microsoft signing key */
                string signerUrl = "https://login.microsoftonline.com/common/discovery/v2.0/keys";

                HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(signerUrl);
                webRequest.Accept = "application/json";
                webRequest.Method = "GET";
                try
                {
                    Dictionary<string, X509Certificate2> kidSigners = new Dictionary<string, X509Certificate2>();
                    Dictionary<string, X509Certificate2> x5tSigners = new Dictionary<string, X509Certificate2>();
                    var webResponse = webRequest.GetResponse();
                    var responseStream = webResponse.GetResponseStream();
                    var sr = new StreamReader(responseStream, Encoding.Default);
                    var json = sr.ReadToEnd();
                    Dictionary<string, List<Dictionary<string, object>>> keys = new JavaScriptSerializer().Deserialize<Dictionary<string, List<Dictionary<string, object>>>>(json);
                    foreach (var k in keys["keys"])
                    {
                        byte[] x5c = Convert.FromBase64String((k["x5c"] as System.Collections.ArrayList)[0] as string);
                        X509Certificate2 x509 = new System.Security.Cryptography.X509Certificates.X509Certificate2(x5c);
                        kidSigners[k["kid"].ToString()] = x509;
                        x5tSigners[k["x5t"].ToString()] = x509;
                    }
                    Cache.Add("MicrosoftOnlineSigner", new Dictionary<string, Dictionary<string, X509Certificate2>> { { "kid", kidSigners }, { "x5t", x5tSigners } }, null, System.Web.Caching.Cache.NoAbsoluteExpiration, new TimeSpan(1, 0, 0), System.Web.Caching.CacheItemPriority.Default, null);
                }
                catch { }
            }
            return Cache["MicrosoftOnlineSigner"] as Dictionary<string, Dictionary<string, X509Certificate2>>;
        }

        private bool VerifyRS256JWT(string header, string payload, byte[] sig, X509Certificate2 cert)
        {
            if (sig == null || cert == null) return false;
            RSACryptoServiceProvider csp = (RSACryptoServiceProvider)cert.PublicKey.Key;
            var sha256 = new SHA256Managed();
            var pkcs1 = new RSAPKCS1SignatureDeformatter(cert.PublicKey.Key);
            pkcs1.SetHashAlgorithm("SHA256");
            byte[] hash = sha256.ComputeHash(System.Text.UTF8Encoding.UTF8.GetBytes(header + "." + payload));
            return pkcs1.VerifySignature(hash, sig);
        }

        protected string GetAzureLoginID(string code, string id_token)
        {
            Func<string, byte[]> base64UrlDecode = s => Convert.FromBase64String(s.Replace('-', '+').Replace('_', '/') + (s.Length % 4 > 1 ? new string('=', 4 - s.Length % 4) : ""));
            Dictionary<string, Dictionary<string, X509Certificate2>> signers = GetMicrosoftOnLineSigners();
            if (!string.IsNullOrEmpty(id_token))
            {
                string[] jwt_segments = id_token.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
                Dictionary<string, object> header = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(System.Text.UTF8Encoding.UTF8.GetString(base64UrlDecode(jwt_segments[0])));
                Dictionary<string, object> payload = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(System.Text.UTF8Encoding.UTF8.GetString(base64UrlDecode(jwt_segments[1])));
                byte[] sig = jwt_segments.Length > 2 ? base64UrlDecode(jwt_segments[2]) : null;
                X509Certificate2 cert = null;
                if (header["alg"].ToString() == "RS256")
                {
                    if (signers["kid"].ContainsKey(header["kid"].ToString())) cert = signers["kid"][header["kid"].ToString()];
                    //if (signers["x5t"].ContainsKey(header["x5t"].ToString())) cert = signers["x5t"][header["x5t"].ToString()];
                }
                bool validSig = VerifyRS256JWT(jwt_segments[0], jwt_segments[1], sig, cert);
                if (validSig)
                {
                    return payload["unique_name"].ToString();
                }
                else return "";
            }
            else
            {
                string clientID = Config.AzureAPIClientId;
                string secret = Config.AzureAPIScret;
                string resource_uri = "https://graph.windows.net";
                string replyUrl = Config.AzureAPIRedirectUrl;
                var uri = new Uri("https://login.microsoftonline.com/common/oauth2/token");
                HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(uri);
                webRequest.Accept = "application/json";
                webRequest.Method = "POST";
                webRequest.ContentType = "application/x-www-form-urlencoded";
                byte[] data = Encoding.UTF8.GetBytes(
                    "code=" + code
                    + "&grant_type=" + System.Web.HttpUtility.UrlEncode("authorization_code")
                    + "&client_id=" + System.Web.HttpUtility.UrlEncode(clientID)
                    + "&client_secret=" + System.Web.HttpUtility.UrlEncode(secret)
                    + "&redirect_uri=" + System.Web.HttpUtility.UrlEncode(replyUrl)
                    + "&resource=" + System.Web.HttpUtility.UrlEncode(resource_uri)
                    );
                webRequest.ContentLength = data.Length;
                Stream dataStream = webRequest.GetRequestStream();
                dataStream.Write(data, 0, data.Length);
                dataStream.Close();

                try
                {
                    var webResponse = webRequest.GetResponse();
                    var responseStream = webResponse.GetResponseStream();
                    var sr = new StreamReader(responseStream, Encoding.Default);
                    var json = sr.ReadToEnd();
                    Dictionary<string, object> ret = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(json);
                    string[] id_tokens = ret["id_token"].ToString().Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries).Select(s => System.Text.UTF8Encoding.UTF8.GetString(base64UrlDecode(s))).ToArray();
                    string[] access_tokens = ret["access_token"].ToString().Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries).Select(s => System.Text.UTF8Encoding.UTF8.GetString(base64UrlDecode(s))).ToArray();
                    Dictionary<string, object> credential = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(id_tokens[1]);
                    return credential["unique_name"].ToString();
                }
                catch (WebException e)
                {
                    using (WebResponse response = e.Response)
                    {
                        HttpWebResponse httpResponse = (HttpWebResponse)response;
                        using (Stream responseStream = response.GetResponseStream())
                        using (var reader = new StreamReader(responseStream))
                        {
                            string text = reader.ReadToEnd();
                            if (response.ContentType.StartsWith("application/json"))
                            {
                                Dictionary<string, object> error = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(text);
                                throw new Exception(error["error"].ToString() + " " + error["error_description"].ToString(), e);
                            }
                            else
                            {
                                throw new Exception(text, e);
                            }
                        }
                    }
                }
            }
        }

        protected bool IsEditableControl(Control c)
        {
            return ((c is TextBox || c is ListBox || c is CheckBox || c is DropDownList || c is RoboCoder.WebControls.ComboBox || c is RadioButtonList) && (((WebControl)c).Enabled) && (c.Visible) && !string.IsNullOrEmpty(c.ID));
        }

        protected byte[] ConvertXML2XLS(byte[] content)
        {
            try
            {
                byte[] code = System.Text.Encoding.ASCII.GetBytes(Config.WsConverterKey);
                HMACMD5 hmac = new HMACMD5(code);
                byte[] hash = hmac.ComputeHash(content);
                string hasString = BitConverter.ToString(hash);
                Common3.Converter converter = new Common3.Converter();
                converter.Url = Config.WsConverterUrl;
                byte[] converted = converter.XML2XLS(content, hasString);
                if (converted.Length > 5) return converted;
                else return content;
            }
            catch { return content; }
        }

        public string GetVisitorIPAddress()
        {
            string visitorIPAddress = HttpContext.Current.Request.ServerVariables["HTTP_X_FORWARDED_FOR"];
            if (string.IsNullOrEmpty(visitorIPAddress)) { visitorIPAddress = HttpContext.Current.Request.ServerVariables["REMOTE_ADDR"]; }
            if (string.IsNullOrEmpty(visitorIPAddress)) { visitorIPAddress = HttpContext.Current.Request.UserHostAddress; }
            if (string.IsNullOrEmpty(visitorIPAddress) || visitorIPAddress.Trim() == "::1") { visitorIPAddress = string.Empty; }
            if (string.IsNullOrEmpty(visitorIPAddress))
            {
                string stringHostName = Dns.GetHostName();
                IPHostEntry ipHostEntries = Dns.GetHostEntry(stringHostName);
                IPAddress[] arrIpAddress = ipHostEntries.AddressList;
                try { visitorIPAddress = arrIpAddress[arrIpAddress.Length - 2].ToString(); }
                catch
                {
                    try { visitorIPAddress = arrIpAddress[0].ToString(); }
                    catch
                    {
                        try { arrIpAddress = Dns.GetHostAddresses(stringHostName); visitorIPAddress = arrIpAddress[0].ToString(); }
                        catch { visitorIPAddress = "127.0.0.1"; }
                    }
                }
            }
            return visitorIPAddress;
        }

        public string GetQSHash(string qs)
        {
            /* calculate the HMAC hash of a string based on unique SessionID and LUser Login Name */
            //byte[] code = System.Web.Security.MachineKey.Protect(System.Text.Encoding.UTF8.GetBytes(Session.SessionID + LUser.UsrId.ToString()), "QueryString");
            byte[] sessionSecret = Session["QSSecret"] as byte[];
            if (sessionSecret == null)
            {
                RandomNumberGenerator rng = new RNGCryptoServiceProvider();
                byte[] tokenData = new byte[32];
                rng.GetBytes(tokenData);
                sessionSecret = tokenData;
                Session["QSSecret"] = tokenData;
            }
            //byte[] code = System.Text.Encoding.ASCII.GetBytes(sessionSecret + LUser.UsrId.ToString());
            byte[] code = sessionSecret;
            System.Security.Cryptography.HMACSHA256 hmac = new System.Security.Cryptography.HMACSHA256(code);
            byte[] hash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(Convert.ToBase64String(code) + qs.ToString()));
            string hashString = BitConverter.ToString(hash);
            return hashString.Replace("-", "");
        }

        public string GetQSHash(System.Collections.Specialized.NameValueCollection qs)
        {
            List<string> param = new List<string>();
            foreach (string key in qs)
            {
                if (key.ToLower() != "hash" && key.ToLower() != "ssd") param.Add(key.ToLower() + "=" + Request.QueryString[key]);
            }
            return GetQSHash(string.Join("&", param.OrderBy(v => v.ToLower()).ToArray()).ToLower().Trim());
        }

        public FileUploadObj GetImageButtonFileObject(string fileContentJSON)
        {

            if (string.IsNullOrEmpty(fileContentJSON))
            {
                return new FileUploadObj()
                {
                    mimeType = "image/jpeg",
                    fileName = "",
                    base64 = null, // rare case from old data, not even a JSON but just straight binary in base64
                };
            }
            System.Web.Script.Serialization.JavaScriptSerializer jss = new System.Web.Script.Serialization.JavaScriptSerializer();
            jss.MaxJsonLength = Int32.MaxValue;

            if (fileContentJSON.StartsWith("["))
            {
                List<RO.Common3._ReactFileUploadObj> fileList = jss.Deserialize<List<RO.Common3._ReactFileUploadObj>>(fileContentJSON);
                List<FileUploadObj> x = new List<FileUploadObj>();
                foreach (var fileInfo in fileList)
                {
                    return new FileUploadObj()
                    {
                        mimeType = fileInfo.mimeType,
                        fileName = fileInfo.fileName,
                        base64 = fileInfo.base64,
                    };
                }
                return new FileUploadObj()
                {
                    mimeType = "image/jpeg",
                    fileName = "",
                    base64 = null, // rare case from old data, not even a JSON but just straight binary in base64
                };
            }
            else if (fileContentJSON.StartsWith("{"))
            {
                try
                {
                    RO.Common3.FileUploadObj fileInfo = jss.Deserialize<RO.Common3.FileUploadObj>(fileContentJSON);
                    return fileInfo;
                }
                catch
                {
                    return new FileUploadObj()
                    {
                        mimeType = "image/jpeg",
                        fileName = "",
                        base64 = fileContentJSON, // rare case from old data, not even a JSON but just straight binary in base64
                    };
                }
            }
            else
            {
                return new FileUploadObj()
                {
                    mimeType = "image/jpeg",
                    fileName = "",
                    base64 = fileContentJSON, // rare case from old data, not even a JSON but just straight binary in base64
                };
            }
        }
        // constant time comparision to avoid timing attack which is a form of online attack on hash value
        private bool SecureEquals(byte[] a, byte[] b)
        {
            if (ReferenceEquals(a, b)) return true;
            if (a == null || b == null || a.Length != b.Length) return false;
            uint diff = 0;
            for (int i = 0; i < a.Length; i++)
                diff |= (uint)(a[i] ^ b[i]);
            return diff == 0;
        }

        private bool SecureEquals(string s1, string s2)
        {
            byte[] b1 = System.Text.UTF8Encoding.UTF8.GetBytes(s1);
            byte[] b2 = System.Text.UTF8Encoding.UTF8.GetBytes(s2);
            return SecureEquals(b1, b2);
        }

        public void ValidatedQS()
        {
            string qsHash = Request.QueryString["hash"];
            if (!SecureEquals(qsHash, GetQSHash())) throw new HttpException(403, "Accessed denied");
        }

        protected void ValidateQSV2()
        {
            try
            {
                /* must sync wih asmxbase.cs */
                int round = 1;
                string _h = Request.QueryString["_h"];
                byte[] x = base64UrlDecode(_h);
                var jss = new System.Web.Script.Serialization.JavaScriptSerializer();
                RO.Facade3.Auth authObject = GetAuthObject();
                string password = authObject.GetSessionEncryptionKey("", ""); // global non-user specific
                string hashKey = authObject.GetSessionSigningKey("", "");
                string salt = password; // global, no salt
                Dictionary<string, string> request = jss.Deserialize<Dictionary<string, string>>(
                                                System.Text.UTF8Encoding.UTF8.GetString(
                                                Decrypt(x,
                                                System.Text.UTF8Encoding.UTF8.GetBytes(password),
                                                System.Text.UTF8Encoding.UTF8.GetBytes(salt), round)));
                long expiry = long.Parse(request["e"]);

                if (DateTime.UtcNow.ToFileTimeUtc() > expiry) throw new HttpException(403, "Accessed denied");

                List<string> param = new List<string>();
                foreach (string key in Request.QueryString)
                {
                    if (key.ToLower() != "_h"
                        && key.ToLower() != "inline"
                        && key.ToLower() != "ico"
                        )
                    {
                        param.Add(key.ToLower() + "=" + Request.QueryString[key]);
                    }
                }
                string qsToHash = string.Join("&", param.OrderBy(v => v.ToLower()).ToArray()).ToLower().Trim();
                byte[] code = Convert.FromBase64String(request["_s"]);
                System.Security.Cryptography.HMACMD5 hmac = new System.Security.Cryptography.HMACMD5(code);
                byte[] hash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(Convert.ToBase64String(code) + qsToHash.ToString()));
                string hashString = Convert.ToBase64String(hash);
                if (!SecureEquals(hashString, request["_h"])) throw new HttpException(403, "Accessed denied");
                if (LUser == null)
                {
                    AnonymousLogin();
                    int usrId = int.Parse(request["id"]);
                    SetImpersonation(usrId);
                }
            }
            catch (Exception ex)
            {
                ErrorTrace(ex, "error", null, Request);
                throw;
            }
        }

        public string GetQSHash()
        {
            return GetQSHash(Request.QueryString);
        }

        public string GetUrlWithQSHash(string url)
        {
            int questionMarkPos = url.IndexOf('?');
            string path = questionMarkPos >= 0 ? url.Substring(0, questionMarkPos) : url;
            string qs = questionMarkPos >= 0 ? url.Substring(questionMarkPos).Substring(1) : "";

            if (string.IsNullOrEmpty(qs)) return url;
            if (!(path.ToLower().StartsWith("~/dnload.aspx") || path.ToLower().StartsWith("dnload.aspx") || path.ToLower().StartsWith("~/upload.aspx") || path.ToLower().StartsWith("upload.aspx"))) return url;

            /* url with hash to prevent tampering of manual construction, only for dnload.aspx */
            return path + "?" + qs + "&hash=" + GetQSHash(string.Join("&", qs.Split(new char[] { '&' }, StringSplitOptions.RemoveEmptyEntries).OrderBy(v => v.ToLower()).ToArray()).ToLower().Trim());
        }

        protected byte[] Encrypt(byte[] clearData, byte[] password, byte[] salt, int round = 1000)
        {
            System.Security.Cryptography.Rfc2898DeriveBytes pdb = new System.Security.Cryptography.Rfc2898DeriveBytes(password, salt, round);
            System.Security.Cryptography.AesManaged aes = new System.Security.Cryptography.AesManaged();
            aes.Key = pdb.GetBytes(aes.KeySize / 8); pdb.Reset(); aes.IV = pdb.GetBytes(aes.BlockSize / 8);
            using (System.Security.Cryptography.ICryptoTransform enc = aes.CreateEncryptor())
            using (System.IO.MemoryStream ms = new System.IO.MemoryStream())
            using (System.Security.Cryptography.CryptoStream cs = new System.Security.Cryptography.CryptoStream(ms, enc, System.Security.Cryptography.CryptoStreamMode.Write))
            {
                cs.Write(clearData, 0, clearData.Length);
                cs.Close();
                ms.Close();
                return ms.ToArray();
            }
        }

        protected byte[] Decrypt(byte[] encryptedData, byte[] password, byte[] salt, int round = 1000)
        {
            System.Security.Cryptography.Rfc2898DeriveBytes pdb = new System.Security.Cryptography.Rfc2898DeriveBytes(password, salt, round);
            System.Security.Cryptography.AesManaged aes = new System.Security.Cryptography.AesManaged();
            aes.Key = pdb.GetBytes(aes.KeySize / 8); pdb.Reset(); aes.IV = pdb.GetBytes(aes.BlockSize / 8);
            using (System.Security.Cryptography.ICryptoTransform enc = aes.CreateDecryptor())
            using (System.IO.MemoryStream ms = new System.IO.MemoryStream())
            using (System.Security.Cryptography.CryptoStream cs = new System.Security.Cryptography.CryptoStream(ms, enc, System.Security.Cryptography.CryptoStreamMode.Write))
            {
                cs.Write(encryptedData, 0, encryptedData.Length);
                cs.Close();
                ms.Close();
                return ms.ToArray();
            }
        }

        /* this is must be in-sync with AsmxBase.cs version */
        protected RO.Facade3.Auth GetAuthObject()
        {
            string jwtMasterKey = System.Configuration.ConfigurationManager.AppSettings["JWTMasterKey"];
            if (string.IsNullOrEmpty(jwtMasterKey))
            {
                RO.Facade3.Auth.GenJWTMasterKey();
                System.Configuration.ConfigurationManager.AppSettings["JWTMasterKey"] = jwtMasterKey;
                Configuration config = WebConfigurationManager.OpenWebConfiguration("~");
                if (config.AppSettings.Settings["JWTMasterKey"] != null) config.AppSettings.Settings["JWTMasterKey"].Value = jwtMasterKey;
                else config.AppSettings.Settings.Add("JWTMasterKey", jwtMasterKey);
                // save to web.config on production, but silently failed. this would remove comments in appsettings 
                if (Config.DeployType == "PRD") config.Save(ConfigurationSaveMode.Modified);
            }

            var auth = RO.Facade3.Auth.GetInstance(jwtMasterKey);
            return auth;
        }

        protected KeyValuePair<string, string> GetResetLoginUrl(string UsrId, string LoginName, string Email, string keyAs, string userState, string signUpURL, string returnUrl)
        {
            DataRow dr = ((new LoginSystem()).GetSaltedUserInfo(int.Parse(UsrId), LoginName, Email)).Rows[0];
            string emailOnFile = dr["UsrEmail"].ToString();
            byte[] resetTime = System.Text.Encoding.ASCII.GetBytes(DateTime.Now.ToFileTimeUtc().ToString());
            string resetTimeEnc = Convert.ToBase64String(Encrypt(resetTime, dr["UsrPassword"] as byte[], dr["UsrPassword"] as byte[]));
            string loginUrl = (System.Web.Security.FormsAuthentication.LoginUrl ?? "").Replace("/" + Config.AppNameSpace + "/", "");
            if (string.IsNullOrEmpty(loginUrl)) loginUrl = "~/MyAccount.aspx";
            string resetUrlPath =
                ResolveUrlCustom(loginUrl.StartsWith("~/") || loginUrl.StartsWith("/") || loginUrl.StartsWith("http") ? loginUrl : "~/" + loginUrl, !IsProxy(), true);
                //((Config.EnableSsl ? Config.SslUrl : Config.OrdUrl).StartsWith("http") ? (new Uri(Config.EnableSsl ? Config.SslUrl : Config.OrdUrl)) : Request.Url).GetLeftPart(UriPartial.Scheme) + Request.Url.Host + Request.Url.AbsolutePath.ToLower().Replace((signUpURL ?? loginUrl).ToLower(), loginUrl);
            string reset_url = string.Format("{0}" + (resetUrlPath.Contains("?") ? "&" : "?") + "{3}={1}&p={2}{4}",
                resetUrlPath,
                HttpUtility.UrlEncode(dr["UsrId"].ToString()),
                HttpUtility.UrlEncode(resetTimeEnc),
                string.IsNullOrEmpty(keyAs) ? "j" : keyAs,
                userState
                );
            return new KeyValuePair<string, string>(emailOnFile, reset_url + (string.IsNullOrEmpty(returnUrl) ? "" : "&ReturnUrl=" + Server.UrlEncode(returnUrl)));
        }

        protected KeyValuePair<string, bool> ValidateResetUrl(string UsrId)
        {
            DataTable dt = (new LoginSystem()).GetSaltedUserInfo(int.Parse(UsrId), "", "");
            if (dt.Rows.Count > 0)
            {
                DataRow dr = dt.Rows[0];
                string loginName = dr["LoginName"].ToString();
                string userEmail = dr["UsrEmail"].ToString();
                byte[] x = Decrypt(Convert.FromBase64String(Request.QueryString["p"]), dr["UsrPassword"] as byte[], dr["UsrPassword"] as byte[]);
                string y = new string(System.Text.Encoding.ASCII.GetChars(x));
                long resetTime = long.Parse(y);
                if (resetTime < DateTime.Now.AddMinutes(-60).ToFileTimeUtc())
                {
                    return new KeyValuePair<string, bool>(loginName, false);
                }
                return new KeyValuePair<string, bool>(loginName, true);
            }
            return new KeyValuePair<string, bool>("", false);
        }

        protected void SetSecureCookie(string name, Dictionary<string, string> content, int duration)
        {
            string xForwardedFor = Request.Headers["X-Forwarded-For"];
            System.Web.Configuration.SessionStateSection SessionSettings = ConfigurationManager.GetSection("system.web/sessionState") as System.Web.Configuration.SessionStateSection;
            string sessionCookieName = SessionSettings != null ? SessionSettings.CookieName : null;
            string cookieName = name + "_" + sessionCookieName;
            HttpCookie sessionCookie = Request.Cookies[sessionCookieName];
            System.Web.Script.Serialization.JavaScriptSerializer jss = new System.Web.Script.Serialization.JavaScriptSerializer();
            string salt = Guid.NewGuid().ToString().Replace("-", "");
            string value = jss.Serialize(content);
            string cookieContent = salt + "," + Convert.ToBase64String(Encrypt(System.Text.UTF8Encoding.UTF8.GetBytes(value), System.Text.UTF8Encoding.UTF8.GetBytes(Config.DesPassword), System.Text.UTF8Encoding.UTF8.GetBytes(salt)));
            HttpCookie cookie = new HttpCookie(cookieName, cookieContent);
            cookie.HttpOnly = true;
            cookie.Secure = IsSecureConnection() 
                            ||
                            (!string.IsNullOrEmpty(xForwardedFor) 
                            && ((Config.EnableSsl) || Config.ExtBaseUrl.StartsWith("https://"))
                            && !Request.IsLocal);
            cookie.Path = "/";

            if (duration > 0) cookie.Expires = DateTime.UtcNow.AddSeconds(duration);
            else cookie.Expires = new DateTime(0);
            try { Response.Cookies.Remove(cookieName); }
            catch { };
            Response.Cookies.Add(cookie);
        }

        protected Dictionary<string, string> GetSecureCookie(string name)
        {
            try
            {
                HttpCookie cookie = Request.Cookies[name + "_" + "ASP.NET_" + Config.AppNameSpace + "SessionId"];
                System.Web.Script.Serialization.JavaScriptSerializer jss = new System.Web.Script.Serialization.JavaScriptSerializer();
                string[] val = cookie.Value.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                string content = System.Text.UTF8Encoding.UTF8.GetString(Decrypt(Convert.FromBase64String(val[1]), System.Text.UTF8Encoding.UTF8.GetBytes(Config.DesPassword), System.Text.UTF8Encoding.UTF8.GetBytes(val[0])));
                return jss.Deserialize<Dictionary<string, string>>(content);
            }
            catch { return null; }
        }

        protected Dictionary<string, string> GetRequestInfo()
        {
            Dictionary<string, string> requestInfo = new Dictionary<string, string>();
            string applicationPath = Request.ApplicationPath;

            foreach (string x in Request.Headers.Keys)
            {
                requestInfo[x] = Request.Headers[x];
            }
            requestInfo["Host"] = Request.Url.Host;
            requestInfo["ApplicationPath"] = applicationPath;
            requestInfo["Url"] = Request.Url.ToString();
            requestInfo["UserHostAddress"] = Request.UserHostAddress;
            return requestInfo;
        }

        protected string GetSiteUrl(bool bIncludeTitle = false)
        {
            string site = 
                ResolveUrlCustom("", !IsProxy(), true)
                //Request.Url.Scheme + "://" + Request.Url.Host + Request.ApplicationPath 
                + (bIncludeTitle ? " (" + Config.WebTitle + ")" : "");
            return site;
        }

        public KeyValuePair<string, bool> GetCriteriaColumnValue(Control container, string controlName)
        {
            WebControl wc = (WebControl)container.FindControl(controlName);
            bool isList = false;

            string val = "";
            if (wc != null)
            {
                if (wc.GetType() == typeof(RoboCoder.WebControls.ComboBox))
                {
                    val = ((RoboCoder.WebControls.ComboBox)wc).SelectedValue;

                }
                else if (wc.GetType() == typeof(DropDownList))
                {
                    val = ((DropDownList)wc).SelectedValue;
                }
                else if (wc.GetType() == typeof(ListBox))
                {
                    val = string.Join(",", ((ListBox)wc).Items.Cast<ListItem>().Where(x => x.Selected).Select(x => x.Value).ToArray());
                    isList = true;
                }
                else if (wc.GetType() == typeof(CheckBox))
                {
                    val = ((CheckBox)wc).Checked ? "Y" : "N";
                }
                else
                {
                    try
                    {
                        val = ((TextBox)wc).Text;
                    }
                    catch { }
                }
            }
            return new KeyValuePair<string, bool>(val, isList);
        }

        public string GetCriteriaRowFilter(DataTable dt, string columnName, KeyValuePair<string, bool> val)
        {
            string rowFilter = "";

            if (!string.IsNullOrEmpty(val.Key.Trim()) && !string.IsNullOrEmpty(columnName))
            {
                string[] needQuoteType = { "Char", "Date", "Time", "String" };
                bool needQuote = needQuoteType.Any(dt.Columns[columnName].DataType.ToString().Contains);
                if (needQuote)
                {
                    if (val.Value)
                        rowFilter = string.Format("{0} IN ({1}) OR {0} IS NULL", columnName, "'" + val.Key.Replace("'", "''").Replace(",", "','") + "'");
                    else
                        rowFilter = string.Format("{0} = '{1}' OR {0} IS NULL", columnName, val.Key.Replace("'", "''"));
                }
                else
                {
                    if (val.Value)
                    {
                        rowFilter = string.Format("{0} IN ({1}) OR {0} IS NULL", columnName, val);
                    }
                    else
                    {
                        rowFilter = string.Format("{0} = {1} OR {0} IS NULL", columnName, val);
                    }
                }
            }

            return rowFilter;
        }

        public void FilterCriteriaDdl(Control criContainer, DataView dv, DataRowView criDrv)
        {
            if (!string.IsNullOrEmpty(criDrv["DdlFtrColumnName"].ToString()) &&
                dv.Table.Columns.Contains(criDrv["DdlFtrColumnName"].ToString()))
            {
                string tabIndex = criDrv.Row.Table.Columns.Contains("DdlFtrColumnTabIndex") ? criDrv["DdlFtrColumnTabIndex"].ToString() : "";
                KeyValuePair<string, bool> filterColVal = GetCriteriaColumnValue(criContainer, "x" + criDrv["DdlFtrColumnName"].ToString() + tabIndex);
                string rowFilter = GetCriteriaRowFilter(dv.Table, criDrv["DdlFtrColumnName"].ToString(), filterColVal);
                dv.RowFilter = rowFilter;
            }
        }

        protected bool IsProxy()
        {
            var Request = Context.Request;
            string extBasePath = Config.ExtBasePath;
            string extDomain = Config.ExtDomain;
            string extBaseUrl = Config.ExtBaseUrl;
            string xForwardedFor = Request.Headers["X-Forwarded-For"];
            string xOriginalUrl = Request.Headers["X-Orginal-URL"];
            string isaHttps = Request.Headers["Front-End-Https"];
            string host = Request.Url.Host;
            string appPath = Request.ApplicationPath;
            string behindProxy = System.Configuration.ConfigurationManager.AppSettings["BehindProxy"];

            return
                behindProxy == "Y"
                ||
                (
                !string.IsNullOrEmpty(extBasePath)
                && (!string.IsNullOrEmpty(xForwardedFor) || !string.IsNullOrEmpty(isaHttps)))
            //                && appPath.ToLower() != extBasePath.ToLower();
                ;
        }

        protected string ResolveUrlCustom(string relativeUrl, bool isInternal = false, bool withDomain = false)
        {
            var Request = Context.Request;
            string url = ResolveUrl(relativeUrl);
            string extBasePath = Config.ExtBasePath;
            string extDomain = Config.ExtDomain;
            string extBaseUrl = Config.ExtBaseUrl;
            string xForwardedFor = Request.Headers["X-Forwarded-For"];
            string xOriginalUrl = Request.Headers["X-Orginal-URL"];
            string host = Request.Url.Host;
            string appPath = Request.ApplicationPath;
            if (IsProxy()
                 && !isInternal
                 //&& Config.TranslateExtUrl
                 && (
                 url.ToLower().StartsWith(("https://" + host + appPath).ToLower())
                 ||
                 url.ToLower().StartsWith(("http://" + host + appPath).ToLower())
                 ||
                 (url.ToLower().StartsWith((appPath).ToLower()) && appPath != "/")
                 ||
                 (appPath == "/" && url.StartsWith("/"))
                 ||
                 !url.StartsWith("/")
                ))
            {
                Dictionary<string, string> requestHeader = new Dictionary<string, string>();
                foreach (string x in Request.Headers.Keys)
                {
                    requestHeader[x] = Request.Headers[x];
                }
                requestHeader["Host"] = host;
                requestHeader["ApplicationPath"] = appPath;
                url = Utils.transformProxyUrl(url, requestHeader);
                return withDomain ? url : new Regex("^" + GetDomainUrl(false), RegexOptions.IgnoreCase).Replace(url, "");
            }
            else
            {
                return withDomain 
                        ? (url.StartsWith("http") ? url : GetDomainUrl(true) + (url.StartsWith("/") ? "" : "/") + url) 
                        : new Regex("^" + GetDomainUrl(true), RegexOptions.IgnoreCase).Replace(url,"");
            }

        }
        protected string GetDomainUrl(bool isInternal = false)
        {
            var Request = HttpContext.Current.Request;
            string intDomainUrl = ((Request.IsSecureConnection) ? "https://" : "http://")
                        + Request.Url.Host
                        + (Request.Url.IsDefaultPort ? "" : ":" + Request.Url.Port.ToString());

            if (isInternal
                || !IsProxy()
                || string.IsNullOrEmpty(Config.ExtBaseUrl))
                return string.IsNullOrEmpty(Config.IntBaseUrl) ? intDomainUrl : new Regex(Config.IntBasePath + "$").Replace(Config.IntBaseUrl, "");
            else
                return new Regex(Config.ExtBasePath + "$").Replace(Config.ExtBaseUrl, "");
        }
        protected string GetBaseUrl(bool isInternal = false)
        {
            string applicationPath = HttpRuntime.AppDomainAppVirtualPath;
            string intBaseUrl = ((Request.IsSecureConnection) ? "https://" : "http://") 
                        + HttpContext.Current.Request.Url.Host
                        + (HttpContext.Current.Request.Url.IsDefaultPort ? "" : ":" + HttpContext.Current.Request.Url.Port.ToString())
                        + (applicationPath == "/" ? "" : applicationPath);
            string baseUrl = ResolveUrlCustom(intBaseUrl, isInternal);
            return baseUrl.EndsWith("/") ? baseUrl.Left(baseUrl.Length - 1) : baseUrl;
        }
        
        protected string GetExtUrl(string url)
        {
            return ResolveUrlCustom(url,false,true);
        }

        protected Dictionary<string, string> GetProxyInfo()
        {
            Dictionary<string, string> info = new Dictionary<string, string>(){
                                                {"X-Forwarded-For",Request.Headers["X-Forwarded-For"]},
                                                {"X-Forwarded-Host",Request.Headers["X-Forwarded-Host"]},
                                                {"X-Forwarded-Proto",Request.Headers["X-Forwarded-Proto"]},
                                                {"X-Forwarded-Port",Request.Headers["X-Forwarded-Port"]},
                                                {"X-Original-URL",Request.Headers["X-Original-URL"]}
                                            };
            return info;
        }

        protected void Redirect(string url)
        {
            string extBasePath = Config.ExtBasePath;
            string extDomain = Config.ExtDomain;
            string extBaseUrl = Config.ExtBaseUrl;
            string xForwardedFor = Request.Headers["X-Forwarded-For"];
            string xOriginalUrl = Request.Headers["X-Orginal-URL"];
            string host = Request.Url.Host;
            string appPath = Request.ApplicationPath;
            if (IsProxy()
                && Config.TranslateExtUrl
                && 
                (
                 url.ToLower().StartsWith(("https://" + host + appPath).ToLower())
                 ||
                 url.ToLower().StartsWith(("http://" + host + appPath).ToLower())
                 ||
                 (url.ToLower().StartsWith((appPath).ToLower()) && appPath != "/")
                 ||
                 (appPath=="/" && url.StartsWith("/"))
                 ||
                 !url.StartsWith("/")
                 )
                )
            {
                Dictionary<string, string> requestHeader = new Dictionary<string, string>();
                foreach (string x in Request.Headers.Keys)
                {
                    requestHeader[x] = Request.Headers[x];
                }
                requestHeader["Host"] = host;
                requestHeader["ApplicationPath"] = appPath;

                string extUrl = Utils.transformProxyUrl(url, requestHeader);

                Response.Redirect(extUrl);
            }
            else
            {
                Response.Redirect(url);
            }
        }

        protected void Redirect(string url, bool endResponse)
        {
            Response.Redirect(url, endResponse);
        }

        protected bool IsSecureConnection()
        {
            string xForwardedProto = Request.Headers["X-Forwarded-Proto"];
            string xForwardedHttps = Request.Headers["X-Forwarded-Https"];
            string xForwardedFor = Request.Headers["X-Forwarded-For"];
            string isaHttps = Request.Headers["Front-End-Https"];
            return 
                Request.IsSecureConnection 
                || (xForwardedProto ?? "").ToLower() == "https" 
                || (xForwardedHttps ?? "").ToLower() == "on"
                || (isaHttps ?? "").ToLower() == "on"
                || Request.Cookies["secureChannelTest"] != null
                ;
        }

        public static List<string> GetExceptionMessage(Exception ex)
        {
            List<string> msg = new List<string>();
            for (var x = ex; x != null; x = x.InnerException)
            {
                if (x is AggregateException && ((AggregateException)x).InnerExceptions.Count > 1)
                {
                    if (((AggregateException)x).InnerExceptions.Count > 1)
                        foreach (var y in ((AggregateException)x).InnerExceptions)
                        {
                            msg.Add(string.Join("\r\n", GetExceptionMessage(y).ToArray()));
                        }
                }
                else
                {
                    msg.Add(x.Message);
                }
            }
            return msg;
        }

        protected void ErrorTrace(Exception e, string severity, Dictionary<string,string> requestInfo, HttpRequest request = null) 
        {
            string supportEmail = System.Configuration.ConfigurationManager.AppSettings["TechSuppEmail"];
            string subjectServerity = string.IsNullOrEmpty(severity) ? "Error" : severity.Substring(0, 1).ToUpper() + (severity.Length > 1 ? severity.Substring(1) : "");
            if (supportEmail != "none" && supportEmail != string.Empty)
            {
                try
                {
                    string webtitle = System.Configuration.ConfigurationManager.AppSettings["WebTitle"] ?? "";
                    string to = System.Configuration.ConfigurationManager.AppSettings["TechSuppEmail"] ?? "cs@robocoder.com";
                    string from = "cs@robocoder.com";
                    string fromTitle = "";
                    string replyTo = "";
                    string smtpServer = System.Configuration.ConfigurationManager.AppSettings["SmtpServer"];
                    string[] smtpConfig = smtpServer.Split(new char[] { '|' });
                    bool bSsl = smtpConfig[0].Trim() == "true" ? true : false;
                    int port = smtpConfig.Length > 1 ? int.Parse(smtpConfig[1].Trim()) : 25;
                    string server = smtpConfig.Length > 2 ? smtpConfig[2].Trim() : null;
                    string username = smtpConfig.Length > 3 ? smtpConfig[3].Trim() : null;
                    string password = smtpConfig.Length > 4 ? smtpConfig[4].Trim() : null;
                    string domain = smtpConfig.Length > 5 ? smtpConfig[5].Trim() : null;
                    System.Net.Mail.MailMessage mm = new System.Net.Mail.MailMessage();
                    string[] receipients = to.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                    string xForwardedFor = request != null ? request.Headers["X-Forwarded-For"] 
                                            : requestInfo != null && requestInfo.ContainsKey("X-Forwarded-For") ? requestInfo["X-Forwarded-For"] : null;
                    string xForwardedHost = request != null ? request.Headers["X-Forwarded-Host"]
                                            : requestInfo != null && requestInfo.ContainsKey("X-Forwarded-Host") ? requestInfo["X-Forwarded-Host"] : null;
                    string xForwardedProto = request != null ? request.Headers["X-Forwarded-Proto"]
                                            : requestInfo != null && requestInfo.ContainsKey("X-Forwarded-Proto") ? requestInfo["X-Forwarded-Proto"] : null;
                    string xOriginalURL = request != null ? request.Headers["X-Original-URL"]
                                            : requestInfo != null && requestInfo.ContainsKey("X-Original-URL") ? requestInfo["X-Original-URL"] : null;

                    string sourceIP = string.Format("From: {0}, Forwarded for: {1}\r\n\r\n", 
                                request != null ? request.UserHostAddress 
                                : requestInfo != null && requestInfo.ContainsKey("UserHostAddress") ? requestInfo["UserHostAddress"] : null
                                , xForwardedFor);
                    string machine = string.Format("Machine: {0}\r\n\r\n", Environment.MachineName);
                    string usrId = string.Format("User: {0}\r\n\r\n", LUser != null ? LUser.UsrId.ToString() : "");
                    string currentTime = string.Format("Server Time: {0} \r\n\r\n UTC: {1} \r\n\r\n", DateTime.Now.ToString("O"), DateTime.UtcNow.ToString("O"));
                    string roVersion = string.Format("RO Version: {0}\r\n\r\n", ROVersion);
                    var exMessages = GetExceptionMessage(e);
                    Exception innerException = e.InnerException;

                    foreach (var t in receipients)
                    {
                        mm.To.Add(new System.Net.Mail.MailAddress(t.Trim()));
                    }
                    mm.Subject = webtitle + string.Format(" Application {0} ", subjectServerity) 
                            + (request != null ? request.Url.GetLeftPart(UriPartial.Path) 
                            : requestInfo != null && requestInfo.ContainsKey("Url") ? requestInfo["Url"] : "unknown request url"
                            );
                    mm.Body = (request != null ? request.Url.ToString() 
                                            : requestInfo != null && requestInfo.ContainsKey("Url") ? requestInfo["Url"] : "unknown request url"
                            )
                            + "\r\n\r\n"
                            + sourceIP
                            + usrId
                            + machine
                            + currentTime
                            + roVersion
                            + exMessages[exMessages.Count - 1] + "\r\n\r\n" + e.StackTrace + (innerException != null ? "\r\n InnerException: \r\n\r\n" + string.Join("\r\n", exMessages.ToArray()) + "\r\n\r\n" + innerException.StackTrace : "") + "\r\n";
                    mm.IsBodyHtml = false; // must be false or it needs to be properly format for \r\n to <br/>
                    mm.From = new System.Net.Mail.MailAddress(string.IsNullOrEmpty(username) || !(username ?? "").Contains("@") ? from : username, string.IsNullOrEmpty(fromTitle) ? from : fromTitle);    // Address must be the same as the smtp login user.
                    mm.ReplyToList.Add(new System.Net.Mail.MailAddress(string.IsNullOrEmpty(replyTo) ? from : replyTo)); // supplied from would become reply too for the 'sending on behalf of'
                    (new RO.WebRules.WebRule()).SendEmail(bSsl, port, server, username, password, domain, mm);
                    mm.Dispose();   // Error is trapped and reported from the caller.

                }
                catch (Exception ex)
                {
                    // never happen, i.e. do nothing just to get around unnecessary compilation warning
                    if (ex == null) throw;
                }
            }
        }
        protected void ErrorTrace(Exception e, string severity)
        {
            try
            {
                ErrorTrace(e, severity, GetRequestInfo());
            }
            catch {
                ErrorTrace(e, severity, null);
            }
        }
        public static byte[] base64UrlDecode(string s)
        {
            return Convert.FromBase64String(s.Replace('-', '+').Replace('_', '/') + (s.Length % 4 > 1 ? new string('=', 4 - s.Length % 4) : ""));
        }

        public static string base64UrlEncode(byte[] content)
        {
            return Convert.ToBase64String(content).TrimEnd(new char[] { '=' }).Replace('/', '_').Replace('+', '-');

        }
        #region XlsImportExport
        public List<string> GetSheetNames(string fileFullName)
        {
            List<string> names = new List<string>();
            OleDbConnection conn = new OleDbConnection();
            System.Collections.ArrayList al = new System.Collections.ArrayList();
            try
            {
                conn.ConnectionString = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + fileFullName + ";Extended Properties=\"Excel 12.0; HDR=NO; IMEX=1;\"";
                //conn.ConnectionString = "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" + fileFullName + ";Extended Properties=\"Excel 8.0; HDR=NO; IMEX=1;\"";
                conn.Open();
                // Get original sheet order:
                DataTable dt = conn.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, new object[] { null, null, null, "TABLE" });
                DataRow[] drs = dt.Select(dt.Columns[2].ColumnName + " not like '*$Print_Area' AND " + dt.Columns[2].ColumnName + " not like '*$''Print_Area'");
                foreach (DataRow dr in drs) { names.Add(dr["TABLE_NAME"].ToString().Replace("'", string.Empty).Replace("$", string.Empty)); }
            }
            catch (Exception e)
            {
                throw (e);
            }
            finally
            {
                conn.Close(); conn = null;
            }

            return names;
        }
        public string ImportFile(string fileName, string workSheet, string startRow, string fileFullName)
        {
            OleDbConnection conn = new OleDbConnection();
            OleDbDataAdapter da = new OleDbDataAdapter();
            DataTable dt = new DataTable();
            try
            {
                conn.ConnectionString = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + fileFullName + ";Extended Properties=\"Excel 12.0; HDR=NO; IMEX=1;\"";
                //conn.ConnectionString = "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" + fileFullName + ";Extended Properties=\"Excel 8.0; HDR=NO; IMEX=1;\"";
                conn.Open();
                string myQuery = @"SELECT * From [" + workSheet + "$]";
                OleDbCommand myCmd = new OleDbCommand(myQuery, conn);
                da.SelectCommand = myCmd;
                da.Fill(dt);
            }
            catch (Exception e) { throw (e); }
            finally { conn.Close(); conn = null; }
            dt.TableName = workSheet;
            dt = CleanData(dt);
            return dt.DataTableToXml();
        }
        private DataTable CleanData(DataTable dt)
        {
            foreach (DataRow dr in dt.Rows)
            {
                foreach (DataColumn dc in dt.Columns)
                {
                    if (dc.DataType == typeof(string))
                    {
                        string r = "[^\x09\x0A\x0D\x20-\uD7FF\uE000-\uFFFD\u10000-\u10FFFF]";
                        dr[dc.ColumnName] = System.Text.RegularExpressions.Regex.Replace(dr[dc.ColumnName].ToString(), r, "", System.Text.RegularExpressions.RegexOptions.Compiled);
                    }
                }
            }
            return dt;
        }
        #endregion

        #region CI_CD deployment/integration helpers
        protected void EndWebHookRequest(string mimeType, byte[] content, Dictionary<string,string> responseHeader = null)
        {
            try
            {
                Response.Buffer = true;
                Response.ClearHeaders();
                Response.ClearContent();
                Response.ContentType = mimeType;
                if (responseHeader != null)
                {
                    foreach (var h in responseHeader)
                    {
                        Response.AppendHeader(h.Key, h.Value);
                    }
                }
                Response.BinaryWrite(content);
                Response.Flush(); // Sends all currently buffered output to the client.
                Response.End();
            }
            catch (ThreadAbortException ex)
            {
                RO.Common3.Utils.NeverThrow(ex);
            }
            catch (Exception ex)
            {
                RO.Common3.Utils.NeverThrow(ex);
            }
        }
        protected string GitCheckout(string branchOrRef)
        {
            try
            {
                string webRoot = Server.MapPath(@"~/").Replace(@"\", "/");
                string appRoot = webRoot.Replace("/Web/", "");

                string branch = System.Configuration.ConfigurationManager.AppSettings["GitCheckoutBranch"];

                if ((branch ?? "").Contains("/"))
                {
                    // fetch latest
                    try
                    {
                        // .git/config must properly configured(credential in url or else would stall)
                        var fetchResult = Utils.WinProc(@"C:\Program Files\Git\cmd\git.exe", "fetch -v ", true, appRoot);
                        if (fetchResult.Item1 != 0)
                        {
                            throw new Exception(fetchResult.Item3);
                        }
                    }
                    catch (Exception ex)
                    {
                        throw ex;
                    }
                }

                // get change set
                var changedFilesRet =
                        (branch ?? "").Contains("/")
                        ? Utils.WinProc(@"C:\Program Files\Git\cmd\git.exe", "diff --name-status master " + branch + " --", true, appRoot)
                        : Utils.WinProc(@"C:\Program Files\Git\cmd\git.exe", "status -s -uno", true, appRoot);

                // checkout, overwrite all local changes
                var revertChangesRet = Utils.WinProc(@"C:\Program Files\Git\cmd\git.exe"
                                                    , "checkout"
                                                        + (
                                                        string.IsNullOrEmpty(branch)
                                                        ? " HEAD "
                                                        : (branch.Contains("/") ? " " + branch + " -B master "
                                                        : " " + branch + " "
                                                        ))
                                                        + "-f -- "
                                                        , true, appRoot);
                if (revertChangesRet.Item1 != 0)
                {
                    throw new Exception(revertChangesRet.Item3);
                }

                // change summary
                int lastX = 20;
                var lastXcommitLog = Utils.WinProc(@"C:\Program Files\Git\cmd\git.exe"
                    , string.Format("--no-pager log -n {0} --pretty=\"%an %ci %H %s\" -n{0}", lastX)
                    , true, appRoot);
                System.Collections.Generic.List<string> ruleTierProjects = new System.Collections.Generic.List<string>() { 
                    "Access3" 
                    ,"Common3" 
                    ,"Facade3" 
                    ,"License3" 
                    ,"Rule3" 
                    ,"Service3" 
                    ,"SystemFrameWk" 
                    ,"WebControls" 
                    ,"WebRules" 
                    ,"UsrAccess" 
                    ,"UsrRules"
                };
                bool needMSBuild = true || ruleTierProjects
                                        .Where(m => new Regex(string.Format("/{0}/", m), RegexOptions.IgnoreCase).IsMatch(changedFilesRet.Item2))
                                        .Any();
                bool noWebSiteBuild = true;

                System.Collections.Generic.List<string> stdOut = new System.Collections.Generic.List<string>();
                System.Collections.Generic.List<string> stdErr = new System.Collections.Generic.List<string>();
                //bool webSiteBuildSkipped = false;
                if (needMSBuild)
                {
                    int? _runningPid = null;
                    Func<int, string, bool> stdOutHandler = (pid, data) =>
                    {
                        _runningPid = pid;
                        stdOut.Add(data);
                        if (data.Contains("aspnet_compiler.exe") && noWebSiteBuild)
                        {
                            stdOut.Add("Skip website rebuild\r\n");
                            //webSiteBuildSkipped = true;
                            return true;
                        }
                        return false;
                    };
                    Func<int, string, bool> stdErrHandler = (pid, data) =>
                    {
                        _runningPid = pid;
                        stdErr.Add(data);
                        return false;
                    };
                    var publishRet =
                        Utils.WinProc(@"C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe", string.Format("/v:n {0}/{1}.sln", appRoot, Config.AppNameSpace), true, stdOutHandler, stdErrHandler, appRoot);

                }
                else
                {
                    stdOut.Add("No rebuild needed\r\n");
                }

                return ("Changed Files\r\n"
                        + changedFilesRet.Item2
                        + "Last " + lastX.ToString() + " commits\r\n"
                        + lastXcommitLog.Item2.Replace("\n\r","\r").Replace("\r\n","\r").Replace("\n","\r").Replace("\r","\r\n\r\n") 
                        + "\r\n\r\n Build Result\r\n" + string.Join("", stdOut.ToArray()));
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }
        protected bool HasGitRepo()
        {
            string webRoot = Server.MapPath(@"~/").Replace(@"\", "/");
            string appRoot = webRoot.Replace("/Web/", "");
            return (Directory.Exists(appRoot + "/.git"));
        }
        protected Tuple<bool, List<string>, List<string>, List<string>, string> PublishReactModule(string systemAbbr, string systemId, bool gitCommit = true)
        {
            bool published = false;
            List<string> msg = new List<string>();
            List<string> errMsg = new List<string>();
            List<string> gitActions = new List<string>();// git command line arguments, one for each command
            string gitActionWorkingDir = "";

            try
            {
                string webAppRoot = Server.MapPath(@"~/").Replace(@"\", "/");
                string appRoot = webAppRoot.Replace("/Web/", "");
                string reactRootDir = webAppRoot.Replace(@"/Web", "/React");
                string reactTemplateDir = reactRootDir + "/Template";
                string reactModuleDir = reactTemplateDir.Replace("/Template", "/" + systemAbbr);
                string reactModuleNodeModuleDir = reactModuleDir + "/node_modules";
                string homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                string appDataDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string siteApplicationPath = Context.Request.ApplicationPath;
                string machineName = Environment.MachineName;
                string rintagiJSContent =
string.Format(@"
/* this is runtime loading script for actual installation(production) configuration override say putting app to deep directory structure or
 * web service end point not the same as the app loading source
 * typically for situation where the apps are hosted in CDN and/or not at root level of the domain
 * for reactjs configuration, make sure homepage is set to './' so everything generated is relative 
 */
document.Rintagi = {{
  appRelBase:['React','ReactProxy','ReactPort'],  // path this app is serving UNDER(can be multiple), implicitly assume they are actually /Name/, do not put begin/end slash 
  appNS:'', // used for login token sync(shared login when served under the same domain) between apps and asp.net site
  appDomainUrl:'', // master domain this app is targetting, empty/null means the same as apiBasename, no ending slash, design for multiple api endpoint usage(js hosting not the same as webservice hosting)
  apiBasename: '', // webservice url, can be relative or full http:// etc., no ending slash
  useBrowserRouter: false,    // whether to use # based router(default) or standard browser based router(set to true, need server rewrite support, cannot be used for CDN or static file directory)
  appBasename: '{0}/react/{1}', // basename after domain where all the react stuff is seated , no ending slash, only used for browserRouter as basename
  appProxyBasename: '{0}/reactproxy', // basename after domain where all the react stuff is seated , no ending slash, only used for browserRouter as basename
  systemId: {3}                
}}
", siteApplicationPath == "/" ? "/" : siteApplicationPath.Substring(1), systemAbbr, machineName, systemId, siteApplicationPath);
                if (!Directory.Exists(reactModuleDir))
                {
                    errMsg.Add(string.Format("React app for {0} not found at {1}", systemAbbr, reactModuleDir));
                    return new Tuple<bool, List<string>, List<string>, List<string>, string>(false, msg, errMsg, gitActions, reactModuleDir);
                }
                if (System.Configuration.ConfigurationManager.AppSettings["AdvanceReactBuildVersion"] != "N")
                {
                    using (var sr = new System.IO.StreamReader(reactModuleDir + "/package.json", System.Text.UTF8Encoding.UTF8))
                    {
                        System.Web.Script.Serialization.JavaScriptSerializer jss = new System.Web.Script.Serialization.JavaScriptSerializer();
                        dynamic packageJson = jss.DeserializeObject(sr.ReadToEnd());
                        var ver = ((string)((IDictionary<string, object>)packageJson)["version"]).Split(new char[] { '.' });
                        var _x = ver.Select((v, i) => int.Parse(v) + (i == ver.Length - 1 ? 1 : 0)).Select(v => v.ToString()).ToArray();
                        var newVer = string.Join(".", _x);
                        ((IDictionary<string, object>)packageJson)["version"] = newVer;

                        sr.Close();

                        using (var sw = new System.IO.StreamWriter(reactModuleDir + "/src/app/Version.js", false, System.Text.UTF8Encoding.UTF8))
                        {
                            sw.WriteLine(string.Format("export const Version = '{0}';", newVer));
                            sw.Close();
                        }
                        using (var sw = new System.IO.StreamWriter(reactModuleDir + "/package.json", false, new System.Text.UTF8Encoding(false)))
                        {
                            sw.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(packageJson, Newtonsoft.Json.Formatting.Indented).Replace(@"\u003c", "<").Replace(@"\u003e", ">"));
                            //sw.Write(jss.Serialize(packageJson).Replace(@"\u003c", "<").Replace(@"\u003e", ">"));
                            sw.Close();
                        }
                        try
                        {
                            string gitAddAction = string.Format("add {0} {1}", string.Format("package.json"), string.Format("src/app/Version.js"));
                            string gitCommitAction = string.Format("commit -m \"{0}\"", string.Format("advance {1} UI to version {0}", newVer, systemAbbr));
                            if (HasGitRepo())
                            {
                                if (gitCommit)
                                {
                                    var aa = Utils.WinProc(@"C:\Program Files\Git\cmd\git.exe", gitAddAction, true, reactModuleDir);
                                    var bb = Utils.WinProc(@"C:\Program Files\Git\cmd\git.exe", gitCommitAction, true, reactModuleDir);
                                    var cc = Utils.WinProc(@"C:\Program Files\Git\cmd\git.exe", string.Format("push"), true, reactModuleDir);
                                }
                                else
                                {
                                    gitActions.Add(gitAddAction);
                                    gitActions.Add(gitCommitAction);
                                    gitActionWorkingDir = reactModuleDir;
                                    // we don't specify push, that should be done at higher level
                                    // gitActions.Add("push");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            // no git is fine
                            msg.Add(ex.Message);
                        }

                    };
                }

                System.Collections.Generic.List<string> stdIn = new System.Collections.Generic.List<string>();
                System.Collections.Generic.List<string> stdOut = new System.Collections.Generic.List<string>();
                int? _runningPid = null;
                Func<int, string, bool> stdInHandler = (pid, output) =>
                {
                    _runningPid = pid;
                    return false;
                };
                Func<int, string, bool> stdErrHandler = (pid, output) =>
                {
                    _runningPid = pid;
                    return false;
                };

                string npmPath = @"C:\Program Files\nodejs\npm.cmd";
                if (!System.IO.File.Exists(reactModuleDir + "/.npmrc"))
                {
                    using (var sr = new System.IO.StreamWriter(reactModuleDir + "/.npmrc", false, System.Text.UTF8Encoding.UTF8))
                    {
                        sr.WriteLine(string.Format("prefix={0}npm", reactRootDir.Replace("/", @"\")));
                        sr.WriteLine(string.Format("cache={0}npm-cache", reactRootDir.Replace("/", @"\")));
                        sr.WriteLine(string.Format("update-notifier=false", reactRootDir.Replace("/", @"\")));
                        sr.Flush();
                        sr.Close();
                    }
                }
                //var ret1 = WinProc(npmPath, "cache clean --force", true, reactModuleDir);
                var npmInstallRet = Utils.WinProc(npmPath, @"install  --no-optional --no-update-notifier", true, stdInHandler, stdErrHandler, reactModuleDir);
                var npmRunBuildRet = Utils.WinProc(npmPath, "run build", true, stdInHandler, stdErrHandler, reactModuleDir);
                var buildDir = reactModuleDir + "/build";
                var webSiteTargetDir = webAppRoot + "/React/" + systemAbbr;
                bool isReady = npmRunBuildRet.Item2.Contains("The build folder is ready to be deployed");

                if ((npmRunBuildRet.Item1 != 0 || npmRunBuildRet.Item3.Contains("ERR")))
                {
                    errMsg.Add(npmRunBuildRet.Item3);
                }
                else
                {

                    var webSiteRuntimeDir = string.Format("{0}/runtime", webSiteTargetDir);
                    var webSiteRuntimeJS = string.Format("{0}/rintagi.js", webSiteRuntimeDir);
                    var publishRet = Utils.WinProc("robocopy.exe", string.Format("{0} {1} /MIR /XF rintagi.js", buildDir, webSiteTargetDir), true, appRoot);
                    if (publishRet.Item1 >= 8)
                    {
                        // weird robocopy return code for error
                        errMsg.Add(publishRet.Item3 + "\r\n" + publishRet.Item2);
                    }
                    else
                    {
                        if (File.Exists(webSiteRuntimeJS))
                        {
                            if (!Directory.Exists(webSiteRuntimeDir)) Directory.CreateDirectory(webSiteRuntimeDir);
                            using (var sr = new StreamWriter(webSiteRuntimeJS, false, System.Text.UTF8Encoding.UTF8))
                            {
                                sr.WriteLine(rintagiJSContent);
                                sr.Close();
                            }
                        }
                        published = true;
                        msg.Add(string.Format("React app for {0} deployed to {1}", systemAbbr, webSiteTargetDir));

                        string gitAddAction = string.Format("add .");
                        string gitCommitAction = string.Format("commit -m \"{0}\"", string.Format("revised React {0} module after npm build", systemAbbr));
                        if (HasGitRepo())
                        {
                            if (gitCommit)
                            {
                                var aa = Utils.WinProc(@"C:\Program Files\Git\cmd\git.exe", gitAddAction, true, reactModuleDir);
                                var bb = Utils.WinProc(@"C:\Program Files\Git\cmd\git.exe", gitCommitAction, true, reactModuleDir);
                                var cc = Utils.WinProc(@"C:\Program Files\Git\cmd\git.exe", string.Format("push"), true, reactModuleDir);
                            }
                            else
                            {
                                gitActions.Add(gitAddAction);
                                gitActions.Add(gitCommitAction);
                                gitActionWorkingDir = reactModuleDir;
                                // we don't specify push, that should be done at higher level
                                // gitActions.Add("push");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                errMsg.Add(systemAbbr + "\r\n" + ex.Message + "\r\n" + ex.StackTrace);
            }
            return new Tuple<bool, List<string>, List<string>, List<string>, string>(published, msg, errMsg, gitActions, gitActionWorkingDir);
        }


        protected Task<Tuple<bool, List<string>, List<string>, List<string>, string>[]> PublishReactAsync(string modules, Dictionary<string, string> requestInfo, bool gitCommit = true)
        {
            string webAppRoot = Server.MapPath(@"~/").Replace(@"\", "/");
            string appRoot = webAppRoot.Replace("/Web/", "");

            string somethingRunning = Application["BuildRunning"] as string;
            Func<string, string, Task<Tuple<bool, List<string>, List<string>, List<string>, string>>> publishReactModule = (systemAbbr, systemId) =>
            {
                return Task.Run<Tuple<bool, List<string>, List<string>, List<string>, string>>(() =>
                {
                    try
                    {
                        var result = PublishReactModule(systemAbbr, systemId, false);
                        if (result.Item1)
                        {
                            ErrorTrace(new Exception(string.Format("React Module {0} published\r\n\r\n{1}"
                                                        , systemAbbr, string.Join("\r\n", result.Item2.ToArray())))
                                                     , "info", requestInfo);
                        }
                        else
                        {
                            ErrorTrace(new Exception(
                                        string.Format("React Module {0} publish failed\r\n\r\n{1}"
                                                , systemAbbr
                                                , string.Join("\r\n", result.Item3.ToArray()))
                                                ), "error", requestInfo);
                        }
                        return result;
                    }
                    catch (Exception ex)
                    {
                        //RO.Common3.Utils.NeverThrow(ex);
                        //return null;
                        throw ex;
                    }
                });
            };
            Func<Task<Tuple<bool, List<string>, List<string>, List<string>, string>[]>> publishAll = async () =>
            {
                Task<Tuple<bool, List<string>, List<string>, List<string>, string>[]> aggregationTask = null;
                try
                {
                    lock (o_lock)
                    {
                        Application["BuildRunning"] = "publishing react";
                    }
                    List<Task<Tuple<bool, List<string>, List<string>, List<string>, string>>> publishTasks = new List<Task<Tuple<bool, List<string>, List<string>, List<string>, string>>>()
                    {
                    };
                    string[] ReactModules = (System.Configuration.ConfigurationManager.AppSettings["PublishReactModules"] ?? "").Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    string[] RequestedModules = string.IsNullOrEmpty(modules) ? null : modules.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    DataTable dtSystems = SystemsList ?? (new LoginSystem()).GetSystemsList(string.Empty, string.Empty);
                    List<string> publishedModules = new List<string>();
                    foreach (DataRow dr in dtSystems.Rows)
                    {
                        string systemAbbr = dr["SystemAbbr"].ToString();
                        if (ReactModules.Contains(systemAbbr, StringComparer.InvariantCultureIgnoreCase) 
                            && 
                            (RequestedModules == null 
                            || RequestedModules.Contains("all", StringComparer.InvariantCultureIgnoreCase)
                            || RequestedModules.Contains(systemAbbr , StringComparer.InvariantCultureIgnoreCase)) 
                           )
                        {
                            publishTasks.Add(publishReactModule(systemAbbr, dr["SystemId"].ToString()));
                            publishedModules.Add(systemAbbr);
                        }
                    }
                    ErrorTrace(new Exception(string.Format("Publishing React module(s) {0}"
                                                , string.Join(",", publishedModules.ToArray())))
                              , "info", requestInfo);
                    aggregationTask = Task.WhenAll(publishTasks.ToArray());
                    Tuple<bool, List<string>, List<string>, List<string>, string>[] x = await aggregationTask;
                    List<string> publishResult = new List<string>();
                    bool hasError = false;
                    bool hasGitAction = false;
                    foreach (var y in x)
                    {
                        hasError = hasError || !y.Item1;
                        publishResult.Add(y.Item1 ? string.Join("", y.Item2.ToArray()) : string.Join("", y.Item3.ToArray()));
                        try
                        {
                            foreach (var z in y.Item4)
                            {
                                try
                                {
                                    Utils.WinProc(@"C:\Program Files\Git\cmd\git.exe", z, true, y.Item5);
                                    hasGitAction = true;
                                }
                                catch { }
                            }
                        }
                        catch { }
                    }
                    try
                    {
                        if (hasGitAction)
                        {
                            var cc = Utils.WinProc(@"C:\Program Files\Git\cmd\git.exe", string.Format("push"), true, appRoot);
                        }
                    }
                    catch { }
                    if (publishResult.Count > 0)
                    {
                        ErrorTrace(new Exception(string.Format("React Publish result \r\n{0}"
                                                    , string.Join("\r\n\r\n", publishResult.ToArray())))
                                  , hasError ? "warning" : "info", requestInfo);
                    }
                    else
                    {
                        ErrorTrace(new Exception(string.Format("React Publish result \r\n{0}"
                                                    , "nothing to publish"))
                                  , hasError ? "warning" : "info", requestInfo);
                    }
                    return x;
                }
                catch (Exception ex)
                {
                    if (aggregationTask != null
                        && aggregationTask.Exception != null
                        && aggregationTask.Exception.InnerException != null
                        && aggregationTask.Exception.InnerExceptions.Any()
                        )
                    {
                        List<string> errors = GetExceptionMessage(aggregationTask.Exception);
                        foreach (var innerEx in aggregationTask.Exception.InnerExceptions)
                        {
                            string error = innerEx.Message;
                        }
                        ErrorTrace(new Exception(string.Format("Publish React Module(s) error \r\n{0}"
                            , string.Join("\r\n\r\n", errors.ToArray())))
                            , "error", requestInfo);
                    }
                    else
                    {
                        List<string> errors = GetExceptionMessage(ex);
                        ErrorTrace(new Exception(string.Format("Publish React Module(s) error \r\n{0}"
                            , string.Join("\r\n\r\n", errors.ToArray())))
                            , "error", requestInfo);
                    }

                    throw ex;
                }
                finally
                {
                    lock (o_lock)
                    {
                        Application["BuildRunning"] = null;
                    }
                }
            };
            if (string.IsNullOrEmpty(somethingRunning))
            {
                return Task.Run(publishAll);
            }
            else
            {
                return null;
            }

        }
        protected void UpdateInstallerSln(string DeployPath)
        {
            System.Collections.ArrayList ToDelete;
            System.Xml.XmlNode xn;
            System.Xml.XmlNodeList xl;
            string InstallProj = "Install.csproj";
            System.Xml.XmlDocument xd = new System.Xml.XmlDocument();
            xd.Load(DeployPath + InstallProj);

            // Step 1: Remove all existing EmbeddedResource and None:
            ToDelete = new System.Collections.ArrayList();
            xl = xd.GetElementsByTagName("EmbeddedResource");
            foreach (System.Xml.XmlNode node in xl)
            {
                if (node.Attributes != null)
                {
                    string attr = node.Attributes[0].Value;
                    if (attr.Contains(".zip") || attr.Contains(".bat") || attr.Contains(".sql")) { ToDelete.Add(node); }
                }
            }
            xl = xd.GetElementsByTagName("None");
            foreach (System.Xml.XmlNode node in xl)
            {
                if (node.Attributes != null)
                {
                    string attr = node.Attributes[0].Value;
                    if (attr.Contains(".zip") || attr.Contains(".bat") || attr.Contains(".sql")) { ToDelete.Add(node); }
                }
            }
            for (int ii = 0; ii < ToDelete.Count; ii++)
            {
                xn = (System.Xml.XmlNode)ToDelete[ii];
                xn.ParentNode.RemoveChild(xn);
            }

            // Step 2: Remove all empty ItemGroup:
            ToDelete = new System.Collections.ArrayList();
            xl = xd.GetElementsByTagName("ItemGroup");
            foreach (System.Xml.XmlNode node in xl)
            {
                if (!node.HasChildNodes) { ToDelete.Add(node); }
            }
            for (int ii = 0; ii < ToDelete.Count; ii++)
            {
                xn = (System.Xml.XmlNode)ToDelete[ii];
                xn.ParentNode.RemoveChild(xn);
            }

            //Step 3: Embedded ncessary resources:
            xn = xd.DocumentElement;
            System.Xml.XmlNode NewItemNode = xd.CreateNode(System.Xml.XmlNodeType.Element, "ItemGroup", string.Empty);
            DirectoryInfo di = new DirectoryInfo(DeployPath);
            foreach (DirectoryInfo dir in di.GetDirectories())
            {
                if (dir.Name != "bin" && dir.Name != "obj")
                {
                    RO.Common3.Utils.SearchDirX("*.bat", dir, NewItemNode, xd, DeployPath);
                    RO.Common3.Utils.SearchDirX("*.sql", dir, NewItemNode, xd, DeployPath);
                    RO.Common3.Utils.SearchDirX("*.zip", dir, NewItemNode, xd, DeployPath);
                    // Two directories deep for now:
                    foreach (DirectoryInfo dir1 in dir.GetDirectories())
                    {
                        RO.Common3.Utils.SearchDirX("*.bat", dir1, NewItemNode, xd, DeployPath);
                        RO.Common3.Utils.SearchDirX("*.sql", dir1, NewItemNode, xd, DeployPath);
                        RO.Common3.Utils.SearchDirX("*.zip", dir1, NewItemNode, xd, DeployPath);
                        foreach (DirectoryInfo dir2 in dir1.GetDirectories())
                        {
                            RO.Common3.Utils.SearchDirX("*.bat", dir2, NewItemNode, xd, DeployPath);
                            RO.Common3.Utils.SearchDirX("*.sql", dir2, NewItemNode, xd, DeployPath);
                            RO.Common3.Utils.SearchDirX("*.zip", dir2, NewItemNode, xd, DeployPath);
                        }
                    }
                }
            }
            xn.AppendChild(NewItemNode);
            xd.Save(DeployPath + InstallProj);

            //for some reason .net leaves  xmlns="" which VS.NET doesnt like so we need to remove it
            StreamReader sr = new StreamReader(DeployPath + InstallProj);
            StringBuilder csproj = new StringBuilder();
            string line;
            while ((line = sr.ReadLine()) != null) { csproj.AppendLine(line); }
            sr.Close();
            StreamWriter sw = new StreamWriter(DeployPath + InstallProj, false);
            sw.Write(csproj.ToString().Replace(" xmlns=\"\"", ""));
            sw.Close();

        }

        protected Task<Tuple<bool, string, string, string, string>> CreateInstallerAsync(short upgradeReleaseId, short newReleaseId, string DeployPath, string releaseTypeAbbr, string package, string SysConnString, string SysAppPw, Dictionary<string, string> requestInfo)
        {
            string webAppRoot = Server.MapPath(@"~/").Replace(@"\", "/");
            string appRoot = webAppRoot.Replace("/Web/", "");
            string somethingRunning = Application["BuildRunning"] as string;
            string deployPath = DeployPath;
            string deployName = deployPath;
            string lockFilePath = DeployPath + "/build.lock";
            Func<short, Task<Tuple<bool, string>>> createPackage = (releaseId) =>
            {
                return Task.Run<Tuple<bool, string>>(() =>
                {
                    try
                    {
                        if (releaseId > 0 &&
                            (new AdmPuMkDeploySystem()).UpdReleaseBuild(releaseId, (new LoginSystem()).GetRbtVersion()))
                        {
                            RO.Rule3.Deploy dp = new RO.Rule3.Deploy();
                            CurrSrc src = new CurrSrc(true, null);
                            CurrTar tar = new CurrTar(true, null);
                            string sbWarnMsg = dp.PrepInstall(releaseId, src, tar, SysConnString, SysAppPw);
                            return new Tuple<bool, string>(true, sbWarnMsg);
                        }
                        return new Tuple<bool, string>(true, "");
                    }
                    catch (Exception ex)
                    {
                        //RO.Common3.Utils.NeverThrow(ex);
                        //return null;
                        throw ex;
                    }
                });
            };
            Func<Task<Tuple<bool, string, string, string, string>>> createInstaller = async () =>
            {
                Task<Tuple<bool, string>[]> aggregationTask = null;
                try
                {
                    lock (o_lock)
                    {
                        Application["BuildRunning"] = "creating installer";
                    }
                    List<Task<Tuple<bool, string>>> createPackageTasks = new List<Task<Tuple<bool, string>>>()
                    {
                    };
                    try
                    {
                        File.Create(lockFilePath);
                    }
                    catch { }

                    if (newReleaseId > 0)
                    {
                        var newPackage = createPackage(newReleaseId);
                        createPackageTasks.Add(newPackage);
                        if (upgradeReleaseId > 0)
                        {
                            // must be sync as they write to same files !
                            await newPackage;
                            createPackageTasks.Add(createPackage(upgradeReleaseId));
                        }
                    }
                    else if (upgradeReleaseId > 0)
                        createPackageTasks.Add(createPackage(upgradeReleaseId));
                    ErrorTrace(new Exception(string.Format("Creating Installer {0}"
                                                , deployName))
                              , "info", requestInfo);
                    aggregationTask = Task.WhenAll(createPackageTasks.ToArray());
                    Tuple<bool, string>[] x = await aggregationTask;
                    bool hasError = false;
                    UpdateInstallerSln(deployPath);
                    string cmd_path = "\"" + Config.BuildExe + "\"";
                    string cmd_arg = "\"" + deployPath + "Install.sln\" /p:Configuration=Release /t:Rebuild /v:minimal /nologo";
                    string compileResult = Utils.WinProc(cmd_path, cmd_arg, true);
                    string dropInstallerLocation = System.Configuration.ConfigurationManager.AppSettings["DeployDropLocation"];
                    string installerPath = "";
                    string dropError = "";
                    string dropToPathName = "";
                    string installerFileName = "";
                    if (compileResult.IndexOf("failed") >= 0
                        || compileResult.Replace("errorreport", string.Empty).Replace("warnaserror", string.Empty).IndexOf("error") >= 0)
                    {
                        hasError = true;
                    }
                    else
                    {
                        installerPath = new Regex(@"Install\s*->\s*(.*)\r\n$").Match(compileResult).Groups[1].Value;
                        installerFileName = DateTime.Now.ToString("yyyyMMdd") + "_" 
                                                    + new Regex("^Deploy",RegexOptions.IgnoreCase).Replace(package,"").ToUpper() 
                                                    + "_Install.exe";
                        hasError = false;
                        try
                        {
                            if (!string.IsNullOrWhiteSpace(dropInstallerLocation)) {
                                dropToPathName = dropInstallerLocation + "/" + installerFileName;
                                System.IO.File.Copy(installerPath, dropToPathName, true);
                            }
                        }
                        catch (Exception ex) {
                            dropError = string.Join("\n", GetExceptionMessage(ex).ToArray());

                        }
                    }

                    ErrorTrace(new Exception(string.Format("Create {2} Installer result \r\n{0}\r\n{1}"
                                                , string.Join("\r\n\r\n", compileResult), dropError, package))
                              , hasError ? "warning" : "info", requestInfo);
                    if (!hasError)
                    {
                        //System.IO.File.Copy(compileResult, @"\\RCGIT\Deploy\Canary\" + installerFileName);
                    }
                    return new Tuple<bool, string, string, string, string>(!hasError, package, installerPath, installerFileName, dropToPathName);
                }
                catch (Exception ex)
                {
                    if (aggregationTask != null
                        && aggregationTask.Exception != null
                        && aggregationTask.Exception.InnerException != null
                        && aggregationTask.Exception.InnerExceptions.Any()
                        )
                    {
                        List<string> errors = GetExceptionMessage(aggregationTask.Exception);
                        ErrorTrace(new Exception(string.Format("Create {1} Installer error \r\n{0}"
                            , string.Join("\r\n\r\n", errors.ToArray()), package))
                            , "error", requestInfo);
                        return new Tuple<bool, string, string, string, string>(false, package, string.Join("\r\n\r\n", errors), "", "");
                    }
                    else
                    {
                        List<string> errors = GetExceptionMessage(ex);
                        ErrorTrace(new Exception(string.Format("Create {1} Installer error \r\n{0}"
                            , string.Join("\r\n\r\n", errors.ToArray()), package))
                            , "error", requestInfo);
                        return new Tuple<bool, string, string, string, string>(false, package, string.Join("\r\n\r\n", GetExceptionMessage(ex).ToArray()), "", "");
                    }
                }
                finally
                {
                    lock (o_lock)
                    {
                        Application["BuildRunning"] = null;
                    }
                    try
                    {
                        File.Delete(lockFilePath);
                    }
                    catch
                    {
                    }
                }
            };

            bool createInProgress = File.Exists(lockFilePath) && new FileInfo(lockFilePath).LastWriteTimeUtc.AddMinutes(30) > DateTime.UtcNow;

            if (
                string.IsNullOrEmpty(somethingRunning)
                ||
                (somethingRunning == "creating installer" || !createInProgress)
                )
            {
                return Task.Run(createInstaller);
            }
            else
            {
                return Task.Run(() => { return new Tuple<bool, string, string, string, string>(false, package, "another building in progress", "", ""); });
            }

        }

        protected Task<string> GitCheckOutAsync(string _branch, Dictionary<string,string> requestInfo)
        {
            string branch = System.Configuration.ConfigurationManager.AppSettings["GitCheckoutBranch"];
            string somethingRunning = Application["BuildRunning"] as string;
            Func<Task<string>> gitResetFromRepo = () =>
            {
                return Task.Run(() =>
                {
                    try
                    {
                        lock (o_lock)
                        {
                            Application["BuildRunning"] = "git reset";
                        }
                        string result = GitCheckout(branch);
                        string checkoutMsg = result.Replace("\r\n", "\r").Replace("\n", "\r").Replace("\r", "\r\n");
                        ErrorTrace(new Exception(string.Format("git checkout {0}\r\n{1}", branch, checkoutMsg)), "info", requestInfo);
                    }
                    catch (Exception ex)
                    {
                        List<string> errors = GetExceptionMessage(ex);
                        ErrorTrace(new Exception(string.Format("GitCheckout error \r\n{0}"
                            , string.Join("\r\n\r\n", errors.ToArray())))
                            , "error", requestInfo);
                    }
                    finally
                    {
                        lock (o_lock)
                        {
                            Application["BuildRunning"] = null;
                        }
                    }
                    return branch;

                }
                );
            };
            if (string.IsNullOrEmpty(somethingRunning))
            {
                return gitResetFromRepo();
            }
            else
            {
                return null;
            }

        }
        #endregion
    }
}