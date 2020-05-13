﻿using System;
using System.Data;
using System.Web;
using System.Web.Services;
using RO.Facade3;
using RO.Common3;
using RO.Common3.Data;
using System.Collections;
using System.IO;
using System.Text;
using System.Net;
using System.Web.Script.Services;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;
using System.Web.Script.Serialization;
using System.Security.Cryptography;
using System.Web.Configuration;
using System.Configuration;
using System.Threading.Tasks;

namespace RO.Web
{

    [ScriptService()]
    [WebService(Namespace = "http://Rintagi.com/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    public abstract class AsmxBase : WebService
    {
        //public class RintagiLoginToken
        //{
        //    public int UsrId { get; set; }
        //    public string LoginName { get; set; }
        //    public string UsrName { get; set; }
        //    public string UsrEmail { get; set; }
        //    public string UsrGroup { get; set; }
        //    public string RowAuthority { get; set; }
        //    public int CompanyId { get; set; }
        //    public int ProjectId { get; set; }
        //    public byte SystemId { get; set; }
        //    public int DefCompanyId { get; set; }
        //    public int DefProjectId { get; set; }
        //    public byte DefSystemId { get; set; }
        //    public byte DbId { get; set; }
        //    public string Resources { get; set; }
        //}

        //public class RintagiLoginJWT
        //{
        //    public string loginId;
        //    public string loginToken;
        //    public int iat;
        //    public int exp;
        //    public int nbf;
        //    public string handle { get; set; }
        //}
        private static object o_lock = new object();
        private static string ROVersion = null;
        private string PageUrlBase;
        private string IntPageUrlBase;
        protected enum IncludeBLOB { None, Icon, Content, DownloadLink };
        private string SystemListCacheWatchFile { get {return Server.MapPath("~/RefreshSystemList.txt");}}

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

        private const String KEY_EntityList = "Cache:EntityList";
        private const String KEY_CompanyList = "Cache:CompanyList";
        private const String KEY_ProjectList = "Cache:ProjectList";
        private const String KEY_CultureList = "Cache:CultureList";

        protected UsrImpr LImpr {get;set;}
        protected UsrCurr LCurr {get;set;}
        protected LoginUsr LUser {get;set;}
        protected byte LcSystemId {get;set;}
        protected string LcSysConnString {get;set;}
        protected string LcAppConnString {get;set;}
        protected string LcAppDb {get;set;}
        protected string LcDesDb {get;set;}
        protected string LcAppPw {get;set;}
        protected CurrPrj CPrj {get;set;}
        protected CurrSrc CSrc {get;set;}
        protected CurrTar CTar {get;set;}

        protected List<string> _CurrentScreenCriteria = null;

        protected string loginHandle;
        protected abstract byte GetSystemId();
        protected abstract int GetScreenId();
        protected abstract string GetProgramName();
        protected abstract string GetValidateMstIdSPName();
        protected abstract string GetMstTableName(bool underlying = true);
        protected abstract string GetDtlTableName(bool underlying = true);
        protected abstract string GetMstKeyColumnName(bool underlying = false);
        protected abstract string GetDtlKeyColumnName(bool underlying = false);
        protected abstract Dictionary<string, SerializableDictionary<string, string>> GetDdlContext();
        protected abstract SerializableDictionary<string, string> InitMaster();
        protected abstract SerializableDictionary<string, string> InitDtl();
        protected abstract DataTable _GetMstById(string pid);
        protected abstract DataTable _GetDtlById(string pid,int screenFilterId);

        public abstract ApiResponse<List<SerializableDictionary<string, string>>, SerializableDictionary<string, AutoCompleteResponse>> GetNewMst();
        public abstract ApiResponse<AutoCompleteResponse, SerializableDictionary<string, AutoCompleteResponse>> GetSearchList(string searchStr, int topN, string filterId, SerializableDictionary<string, string> desiredScreenCriteria);
        public abstract ApiResponse<List<SerializableDictionary<string, string>>, SerializableDictionary<string, AutoCompleteResponse>> GetDtlById(string keyId, SerializableDictionary<string, string> options, int filterId);
        public abstract ApiResponse<List<SerializableDictionary<string, string>>, SerializableDictionary<string, AutoCompleteResponse>> GetMstById(string keyId, SerializableDictionary<string, string> options);

        protected virtual bool AllowAnonymous() { return false; }
        protected virtual byte GetDbId() { return GetSystemId(); }

        static protected List<string> LisSuggestsOptions = new List<string>() { "startKeyVal", "startLabelVal" };

        protected static int siteInfoCacheInMinutes = Config.DeployType == "PRD" ? 60 : 1;
        protected static int metaDataCacheInMinutes = Config.DeployType == "PRD" ? 30 : 1;
        protected static int volatileMetaDataCacheInMinutes = Config.DeployType == "PRD" ? 5 : 1;  

        protected static int systemListCacheMinutes = siteInfoCacheInMinutes;
        protected static int entityListCacheMinutes = siteInfoCacheInMinutes;

        protected static int systemLabelCacheMinutes = metaDataCacheInMinutes;
        protected static int screenButtonCacheMinutes = metaDataCacheInMinutes;
        protected static int screenCriteriaCacheMinutes = metaDataCacheInMinutes;
        protected static int screenCriHlpCacheMinutes = metaDataCacheInMinutes;
        protected static int screenInCriCacheMinutes = metaDataCacheInMinutes;
        protected static int screenTabCacheMinutes = metaDataCacheInMinutes;
        protected static int screenHlpCacheMinutes = metaDataCacheInMinutes;
        protected static int screenFilterCacheMinutes = metaDataCacheInMinutes;
        protected static int screenAuthColCacheMinutes = metaDataCacheInMinutes;
        protected static int screenLabelCacheMinutes = metaDataCacheInMinutes;

        protected static int screenInCacheMinutes = volatileMetaDataCacheInMinutes;
        protected static int screenLstCriCacheMinutes = volatileMetaDataCacheInMinutes;
        protected static int menuCacheMinutes = volatileMetaDataCacheInMinutes;
        protected static int screenMenuCacheMinutes = volatileMetaDataCacheInMinutes;
        protected static int usrImprCacheMinutes = volatileMetaDataCacheInMinutes;
        protected static int usrHandleCacheMinutes = volatileMetaDataCacheInMinutes;
        protected static int screenAuthRowCacheMinutes = volatileMetaDataCacheInMinutes;
        protected static int companyListCacheMinutes = volatileMetaDataCacheInMinutes;
        protected static int projectListCacheMinutes = volatileMetaDataCacheInMinutes;

        public class AsmXResult<ContentClass>
        {
            public ContentClass d;
        }

        public class _ReactFileUploadObj
        {
        // this goes hand in hand with the react file upload control any change there must be reflected here
            public string fileName;
            public string mimeType;
            public Int64 lastModified;
            public string base64;
            public float height;
            public float width;
            public int size;
            public string previewUrl;
        }

        public class FileUploadObj
        {
            public string fileName;
            public string mimeType;
            public Int64 lastModified;
            public string base64;
            public string previewUrl;
            public string icon;
            public float height;
            public float width;
            public int size;
        }
        public class FileInStreamObj
        {
            public string fileName;
            public string mimeType;
            public Int64 lastModified;
            public string ver;
            public float height;
            public float width;
            public int size;
            public string previewUrl; 
            public int extensionSize;
            public bool contentIsJSON = false;
        }

        public class AutoCompleteResponse
        {
            public string query;
            public List<SerializableDictionary<string, string>> data;
            public int total;
            public int topN;
            public int skipped;
            public int matchCount;
        }

        public class AutoCompleteResponseObj
        {
            public string query;
            public SerializableDictionary<string, SerializableDictionary<string, string>> data;
            public int total;
            public int topN;
        }
        public class SaveDataResponse
        {
            public SerializableDictionary<string, string> mst;
            public List<SerializableDictionary<string,string>> dtl;
            public string message;
        }
        public class ApiResponse<T,S>
        {
//            public List<SerializableDictionary<string, string>> data;
            public T data;
            public S supportingData;
            public string status;
            public string errorMsg;
            public List<KeyValuePair<string, string>> validationErrors;
        }

        public class LoginApiResponse
        {
            public string message;
            public string status;
            public string errorMsg;
            public string accessCode;
            public string error;
            public string serverChallenge;
            public int challengeCount;
            public SerializableDictionary<string, string> accessToken;
            public SerializableDictionary<string, string> refreshToken;
        }
        public class LoadScreenPageResponse
        {
            public List<SerializableDictionary<string, string>> AuthRow;
            public List<SerializableDictionary<string, string>> AuthCol;
            public List<SerializableDictionary<string, string>> ColumnDef;
            public List<SerializableDictionary<string, string>> ScreenCriteria;
            public List<SerializableDictionary<string, string>> ScreenCriteriaDef;
            public List<SerializableDictionary<string, string>> ScreenFilter;
            public List<SerializableDictionary<string, string>> ScreenHlp;
            public List<SerializableDictionary<string, string>> ScreenButtonHlp;
            public List<SerializableDictionary<string, string>> Label;
            public SerializableDictionary<string, List<SerializableDictionary<string, string>>> Ddl;
            public List<SerializableDictionary<string, string>> SearchList;
            public SerializableDictionary<string, string> SearchListParam;
            public SerializableDictionary<string, string> Mst;
            public SerializableDictionary<string, List<SerializableDictionary<string, string>>> MstPullUp;
            public List<SerializableDictionary<string, string>> Dtl;
            public SerializableDictionary<string, string> NewMst;
            public SerializableDictionary<string, string> NewDtl;
            public List<SerializableDictionary<string, string>> SystemLabels;
        }
        private static string _masterkey;

        public struct ScreenResponse
        {
            public SerializableDictionary<string, SerializableDictionary<string, object>> data;
            public string status;
            public string errorMsg;
            public List<KeyValuePair<string, string>> validationErrors;
        }

        public struct MenuResponse
        {
            public List<MenuNode> data;
            public string status;
            public string errorMsg;
        }
        #region Subclass override
        protected virtual bool PreSaveMultiDoc(string mstId, string dtlId, bool isMaster, string docId, bool overwrite, string screenColumnName, string tableName, string docJson, SerializableDictionary<string, string> options)
        {
            return true;
        }
        protected virtual bool PostSaveMultiDoc(string mstId, string dtlId, bool isMaster, string docId, bool overwrite, string screenColumnName, string tableName, string docJson, SerializableDictionary<string, string> options)
        {
            return true;
        }
        protected virtual bool PreSaveEmbeddedDoc(string docJson, string keyId, string tableName, string keyColumnName, string columnName)
        {
            return true;
        }
        protected virtual bool PostSaveEmbeddedDoc(string keyId, string tableName, string keyColumnName, string columnName)
        {
            return true;
        }
        protected virtual bool PreDelMultiDoc(string mstId, string dtlId, bool isMaster, string docId, bool overwrite, string screenColumnName, string tableName, string docTableName, SerializableDictionary<string, string> options)
        {
            return true;
        }
        protected virtual bool PostDelMultiDoc(string mstId, string dtlId, bool isMaster, string docId, bool overwrite, string screenColumnName, string tableName, string docTableName, SerializableDictionary<string, string> options)
        {
            return true;
        }
        #endregion
        #region General helper functions
        public static int ToUnixTime(DateTime time)
        {
            var utc0 = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            return (int)DateTime.SpecifyKind(time, DateTimeKind.Utc).Subtract(utc0).TotalSeconds;            
        }
        public static string TranslateISO8601DateTime(string t, bool storeInUTC = true)
        {
            try
            {
                bool hasTZInfo = new Regex("Z$", RegexOptions.IgnoreCase).IsMatch(t) || new Regex(@"[\-\+0-9\:]+$", RegexOptions.IgnoreCase).IsMatch(t);
                if (storeInUTC)
                    return (!hasTZInfo ? DateTime.Parse(t, null, System.Globalization.DateTimeStyles.RoundtripKind) : DateTime.Parse(t, null, System.Globalization.DateTimeStyles.RoundtripKind).ToUniversalTime()).ToString("F");
                else
                    return DateTime.Parse(new Regex(@"Z|([\-\+0-9\:]+)$", RegexOptions.IgnoreCase).Replace(t, ""), null, System.Globalization.DateTimeStyles.RoundtripKind).ToString("F");
            }
            catch { return null; }
        }
        public static bool VerifyHS256JWT(string header, string payload, string base64UrlEncodeSig, string secret)
        {
            Func<byte[], string> base64UrlEncode = (c) => Convert.ToBase64String(c).TrimEnd(new char[] { '=' }).Replace('_', '/').Replace('-', '+');
            HMACSHA256 hmac = new HMACSHA256(System.Text.UTF8Encoding.UTF8.GetBytes(secret));
            byte[] hash = hmac.ComputeHash(System.Text.UTF8Encoding.UTF8.GetBytes(header + "." + payload));
            return base64UrlEncodeSig == base64UrlEncode(hash);
        }
        public static string ToXMLParams(Dictionary<string, string> WrSPCallParams, string xlmRootName = "Params", List<string> onlyInclude = null)
        {
            List<string> x = WrSPCallParams
                    .Where(kvp => onlyInclude == null || onlyInclude.Contains(kvp.Key))
                    .Aggregate(new List<string>(), (a, kvp) => { a.Add(string.Format("<{0}>{1}</{0}>", kvp.Key, kvp.Value)); return a; });
            string xmlParams = string.Format("<{0}>{1}</{0}>",
                    xlmRootName,
                    string.Join("", x.ToArray()));
            return xmlParams;
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
        public static byte[] base64UrlDecode(string s)
        {
            return Convert.FromBase64String(s.Replace('-', '+').Replace('_', '/') + (s.Length % 4 > 1 ? new string('=', 4 - s.Length % 4) : ""));
        }

        public static string base64UrlEncode(byte[] content)
        {
            return Convert.ToBase64String(content).TrimEnd(new char[] { '=' }).Replace('/', '_').Replace('+', '-');

        }
        #endregion
        public class ReCaptcha
        {
            public bool Success { get; set; }
            public List<string> ErrorCodes { get; set; }

            public static bool Validate(string secret, string encodedResponse)
            {
                if (string.IsNullOrEmpty(encodedResponse)) return false;

                var client = new System.Net.WebClient();

                if (string.IsNullOrEmpty(secret)) return false;

                var googleReply = client.DownloadString(string.Format("https://www.google.com/recaptcha/api/siteverify?secret={0}&response={1}", secret, encodedResponse));

                var serializer = new System.Web.Script.Serialization.JavaScriptSerializer();

                var reCaptcha = serializer.Deserialize<ReCaptcha>(googleReply);

                return reCaptcha.Success;
            }
        }

        protected Tuple<string, string, string> GetCurrentCallInfo()
        {
            var Request = HttpContext.Current.Request;
            var Path = Request.Path;
            var FunctionName = Request.PathInfo;
            var ModuleEndPoint = Path.Replace(FunctionName, "");
            var ServiceEndPoint = new Regex("(/.*)/.*$").Replace(ModuleEndPoint, "$1");
            return new Tuple<string, string, string>(ServiceEndPoint, ModuleEndPoint, FunctionName);
        }
        protected ReturnType PostAsmXRequest<ReturnType>(string url, string jsonBodyContent, bool forwardAccessToken = false)
        {
            Uri uri = new Uri(url);
            var CalleeEndPoint = uri.AbsolutePath;
            var CurrentCallInfo = GetCurrentCallInfo();
            var ServiceEndPoint = CurrentCallInfo.Item1;
            string xForwardedHost = HttpContext.Current.Request.Headers["X-Forwarded-Host"];
 
            bool sameEndpoint = (uri.IsLoopback || uri.Host == HttpContext.Current.Request.Url.Host || uri.Host == xForwardedHost) && uri.AbsolutePath.StartsWith(ServiceEndPoint);

            uri.GetLeftPart(UriPartial.Path);
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            var auth = (HttpContext.Current.Request.Headers["Authorization"] ?? HttpContext.Current.Request.Headers["X-Authorization"]) as string;
            var scope = HttpContext.Current.Request.Headers["X-RintagiScope"] as string;

            if (forwardAccessToken || sameEndpoint)
            {
                request.Method = "POST";
                request.Headers.Add("X-Authorization", auth);
                request.Headers.Add("X-RintagiScope", scope);
            }

            System.Text.UTF8Encoding encoding = new System.Text.UTF8Encoding();
            Byte[] byteArray = encoding.GetBytes(jsonBodyContent);

            request.ContentLength = byteArray.Length;
            request.ContentType = @"application/json";

            using (Stream dataStream = request.GetRequestStream())
            {
                dataStream.Write(byteArray, 0, byteArray.Length);
            }
            //long length = 0;
            try
            {
                using (var response = (HttpWebResponse)request.GetResponse())
                {
                    using (var responseStream = response.GetResponseStream())
                    using (var reader = new StreamReader(responseStream, encoding))
                    {
                        string result = reader.ReadToEnd();
                        JavaScriptSerializer jss = new JavaScriptSerializer();
                        AsmXResult<ReturnType> content = jss.Deserialize<AsmXResult<ReturnType>>(result);
                        return content.d;
                    }

                }
            }
            catch (WebException ex)
            {
                // Log exception and throw as for GET example above
                if (ex == null) return default(ReturnType);
            }

            return default(ReturnType);
        }

        #region Authentications
        /* this is must be in-sync with ModuleBase.cs version */
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
        protected string MasterKey_
        {
            get
            {

                if (_masterkey == null)
                {
                    string jwtMasterKey = System.Configuration.ConfigurationManager.AppSettings["JWTMasterKey"];
                    if (string.IsNullOrEmpty(jwtMasterKey))
                    {
                        try
                        {
                            byte[] randomBits = new byte[32];
                            using (RNGCryptoServiceProvider rngCsp = new RNGCryptoServiceProvider())
                            {
                                // Fill the array with a random value.
                                rngCsp.GetBytes(randomBits);
                            }
                            jwtMasterKey = Convert.ToBase64String(randomBits);
                            System.Configuration.ConfigurationManager.AppSettings["JWTMasterKey"] = jwtMasterKey;
                            Configuration config = WebConfigurationManager.OpenWebConfiguration("~");
                            if (config.AppSettings.Settings["JWTMasterKey"] != null) config.AppSettings.Settings["JWTMasterKey"].Value = jwtMasterKey;
                            else config.AppSettings.Settings.Add("JWTMasterKey", jwtMasterKey);
                            // save to web.config on production, but silently failed. this would remove comments in appsettings 
                            if (Config.DeployType == "PRD") config.Save(ConfigurationSaveMode.Modified);
                        }
                        catch
                        {
                            jwtMasterKey = Config.DesPassword;
                        }
                    }
                    RO.Facade3.Auth.GetInstance(jwtMasterKey);
                    // delay brute force attack, 100K round(ethereum keystore use 260K round, 100K round requires about 5 sec as of 2018/6 hardware)
                    //, we only do this once and stored in class variable so there is a 5 sec delay when app started for API usage
                    Rfc2898DeriveBytes k = new Rfc2898DeriveBytes(jwtMasterKey, UTF8Encoding.UTF8.GetBytes(jwtMasterKey), 100000);
                    _masterkey = (new AdminSystem()).EncryptString(Convert.ToBase64String(k.GetBytes(32)));
                }
                return _masterkey;
            }
        }
        //protected string GetSessionEncryptionKey(string time, string usrId)
        //{
        //    System.Security.Cryptography.HMACSHA256 hmac = new System.Security.Cryptography.HMACSHA256(UTF8Encoding.UTF8.GetBytes(MasterKey));
        //    return (new AdminSystem()).EncryptString(Convert.ToBase64String(hmac.ComputeHash(UTF8Encoding.UTF8.GetBytes(MasterKey + usrId + time))));
        //}
        //protected string GetSessionSigningKey(string time, string usrId)
        //{
        //    var key = GetSessionEncryptionKey(time, usrId);
        //    return Convert.ToBase64String(new Rfc2898DeriveBytes(key, UTF8Encoding.UTF8.GetBytes(key), 1).GetBytes(32));
        //}

        //protected Tuple<string, string> GetSignedToken(string nounce)
        //{
        //    System.Security.Cryptography.HMACSHA256 hmac = new System.Security.Cryptography.HMACSHA256(UTF8Encoding.UTF8.GetBytes(MasterKey));
        //    string hash = BitConverter.ToString(hmac.ComputeHash(UTF8Encoding.UTF8.GetBytes(nounce))).Replace("-", "");
        //    return new Tuple<string, string>(hash.Left(6), hash.Substring(6));
        //}
        //protected bool VerifySignedToken(string nounce, string ticketLeft, string ticketRight)
        //{
        //    System.Security.Cryptography.HMACSHA256 hmac = new System.Security.Cryptography.HMACSHA256(UTF8Encoding.UTF8.GetBytes(MasterKey));
        //    string hash = BitConverter.ToString(hmac.ComputeHash(UTF8Encoding.UTF8.GetBytes(nounce))).Replace("-", "");
        //    return hash == ticketLeft.Trim() + ticketRight.Trim();
        //}

        protected UsrImpr SetImpersonation(UsrImpr LImpr, Int32 usrId, byte systemId, Int32 companyId, Int32 projectId)
        {
            UsrImpr ui = null;
            ui = (new LoginSystem()).GetUsrImpr(usrId, companyId, projectId, systemId);
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
                            SetImpersonation(LImpr, Int32.Parse(dr["ImprUsrId"].ToString()), systemId, companyId, projectId);
                        }
                    }
                }
            }
            return LImpr;
        }
        protected string CreateEncryptedLoginToken(LoginUsr usr, int defCompanyId, int defProjectId, byte defSystemId, UsrCurr curr, UsrImpr impr, string resources, string secret)
        {
            RintagiLoginToken loginToken = new RintagiLoginToken()
            {
                UsrId = usr.UsrId,
                LoginName = usr.LoginName,
                UsrName = usr.UsrName,
                UsrEmail = usr.UsrEmail,
                UsrGroup = impr.UsrGroups,
                RowAuthority = impr.RowAuthoritys,
                SystemId = curr.SystemId,
                CompanyId = curr.CompanyId,
                ProjectId = curr.ProjectId,
                DefSystemId = defSystemId,
                DefCompanyId = defCompanyId,
                DefProjectId = defProjectId,
                DbId = curr.DbId,
                Resources = resources,
            };
            string json = new JavaScriptSerializer().Serialize(loginToken);
            MD5CryptoServiceProvider hashmd5 = new MD5CryptoServiceProvider();
            string hash = BitConverter.ToString(hashmd5.ComputeHash(UTF8Encoding.UTF8.GetBytes(json))).Replace("-", "");
            string encrypted = RO.Common3.Utils.ROEncryptString(hash.Left(32) + json, secret);
            return encrypted;
        }
        protected RintagiLoginToken DecryptLoginToken(string encryptedToken, string secret)
        {
            string decryptedToken = RO.Common3.Utils.RODecryptString(encryptedToken, secret);
            RintagiLoginToken token = new JavaScriptSerializer().Deserialize<RintagiLoginToken>(decryptedToken.Substring(32));
            return token;
        }
        protected string CreateLoginJWT(LoginUsr usr, int defCompanyId, int defProjectId, byte defSystemId, UsrCurr curr, UsrImpr impr, string resources, int validSeconds, string guidHandle)
        {
            return GetAuthObject().CreateLoginJWT(usr, defCompanyId, defProjectId, defSystemId, curr, impr, resources, validSeconds, guidHandle);

            //Func<byte[], string> base64UrlEncode = (c) => Convert.ToBase64String(c).TrimEnd(new char[] { '=' }).Replace('_', '/').Replace('-', '+');
            //var utc0 = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            //var issueTime = DateTime.Now.ToUniversalTime();
            //var iat = (int)issueTime.Subtract(utc0).TotalSeconds;
            //var exp = (int)issueTime.AddSeconds(validSeconds).Subtract(utc0).TotalSeconds; // Expiration time is up to 1 hour, but lets play on safe side
            //var encryptionKey = GetSessionEncryptionKey(iat.ToString(), usr.UsrId.ToString());
            //var signingKey = GetSessionSigningKey(iat.ToString(), usr.UsrId.ToString());
            //RintagiLoginJWT token = new RintagiLoginJWT()
            //{
            //    iat = iat,
            //    exp = exp,
            //    nbf = iat,
            //    loginToken = CreateEncryptedLoginToken(usr, defCompanyId, defProjectId, defSystemId, curr, impr, resources, encryptionKey),
            //    loginId = usr.UsrId.ToString(),
            //    handle = guidHandle
            //};
            //string payLoad = new JavaScriptSerializer().Serialize(token);
            //string header = "{\"typ\":\"JWT\",\"alg\":\"HS256\"}";
            //HMACSHA256 hmac = new HMACSHA256(System.Text.UTF8Encoding.UTF8.GetBytes(signingKey));
            //string content = base64UrlEncode(System.Text.UTF8Encoding.UTF8.GetBytes(header)) + "." + base64UrlEncode(System.Text.UTF8Encoding.UTF8.GetBytes(payLoad));
            //byte[] hash = hmac.ComputeHash(System.Text.UTF8Encoding.UTF8.GetBytes(content));
            //return content + "." + base64UrlEncode(hash);
        }
        protected RO.Facade3.RintagiLoginJWT GetLoginUsrInfo(string jwt)
        {
            return GetAuthObject().GetLoginUsrInfo(jwt);
            //string[] x = (jwt ?? "").Split(new char[] { '.' });
            //Func<string, byte[]> base64UrlDecode = s => Convert.FromBase64String(s.Replace('-', '+').Replace('_', '/') + (s.Length % 4 > 1 ? new string('=', 4 - s.Length % 4) : ""));
            //if (x.Length >= 3)
            //{
            //    try
            //    {
            //        Dictionary<string, string> header = new JavaScriptSerializer().Deserialize<Dictionary<string, string>>(System.Text.UTF8Encoding.UTF8.GetString(base64UrlDecode(x[0])));
            //        try
            //        {
            //            RintagiLoginJWT loginJWT = new JavaScriptSerializer().Deserialize<RintagiLoginJWT>(System.Text.UTF8Encoding.UTF8.GetString(base64UrlDecode(x[1])));
            //            string signingKey = GetSessionSigningKey(loginJWT.iat.ToString(), loginJWT.loginId.ToString());
            //            bool valid = header["typ"] == "JWT" && header["alg"] == "HS256" && VerifyHS256JWT(x[0], x[1], x[2], signingKey);
            //            if (valid)
            //            {
            //                return loginJWT;
            //            }
            //            else return null;
            //        }
            //        catch
            //        {
            //            return null;
            //        }
            //    }
            //    catch { return null; }
            //}
            //else return null;

        }
        protected bool ValidateJWTHandle(string handle)
        {
            // can check centralized location for universal logout etc.
            return true;
        }
        #endregion
        protected Dictionary<byte,Dictionary<string,string>> GetSystemsDict(bool ignoreCache=false)
        {
            var context = HttpContext.Current;
            var cache = context.Cache;
            int minutesToCache = systemListCacheMinutes; // 30;
            Dictionary<byte, Dictionary<string, string>> sysDict = cache[KEY_SystemsDict] as Dictionary<byte, Dictionary<string, string>>;

            if (ROVersion == null)
            {
                lock (o_lock)
                {
                    try
                    {
                        ROVersion = (new LoginSystem()).GetRbtVersion();
                    }
                    catch
                    {
                        ROVersion = "unknown";
                    }
                }
            }

            if (sysDict == null) {
                sysDict = new Dictionary<byte, Dictionary<string, string>>();
                DataTable dt = LoadSystemsList(ignoreCache);
                bool singleSQLCredential = (System.Configuration.ConfigurationManager.AppSettings["DesShareCred"] ?? "N") == "Y";
                dt.PrimaryKey = new DataColumn[] { dt.Columns["SystemId"] };
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
                foreach (DataRow dr in dt.Rows)
			    {
				    Dictionary<string,string> dict = new Dictionary<string,string>();
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
                    sysDict[byte.Parse(dr["SystemId"].ToString())] = dict;
                }
                cache.Add(KEY_SystemsDict,sysDict,new System.Web.Caching.CacheDependency(SystemListCacheWatchFile)
                    , System.Web.Caching.Cache.NoAbsoluteExpiration, new TimeSpan(0, minutesToCache, 0), System.Web.Caching.CacheItemPriority.AboveNormal, null);
            }
            return sysDict;
        }

        protected string SysCustServEmail(byte SystemId)
        {
            try { return GetSystemsDict()[SystemId][KEY_SysCustServEmail]; }
            catch { return string.Empty; }
        }

        protected String SysAdminEmail(byte SystemId)
        {
            try { return GetSystemsDict()[SystemId][KEY_SystemsDict]; } catch { return string.Empty; }
        }

        protected DataTable LoadSystemsList(bool ignoreCache=false)
        {
            var context = HttpContext.Current;
            var cache = context.Cache;
            int minutesToCache = systemListCacheMinutes; // 30;

            DataTable dt = cache[KEY_SystemsList] as DataTable;

            if (dt == null || ignoreCache) {
                dt = (new LoginSystem()).GetSystemsList(string.Empty, string.Empty);
                dt.PrimaryKey = new DataColumn[] { dt.Columns["SystemId"] };
                cache.Insert(KEY_SystemsList, dt, new System.Web.Caching.CacheDependency(SystemListCacheWatchFile)
                    , System.Web.Caching.Cache.NoAbsoluteExpiration, new TimeSpan(0, minutesToCache, 0), System.Web.Caching.CacheItemPriority.AboveNormal, null);
            }
            return dt;
        }
        protected DataTable LoadEntityList(bool ignoreCache)
        {
            var context = HttpContext.Current;
            var cache = context.Cache;
            int minutesToCache = entityListCacheMinutes; //30;

            DataTable dt = cache[KEY_EntityList] as DataTable;
            if (dt == null || ignoreCache)
            {
                dt = (new LoginSystem()).GetSystemsList(string.Empty, string.Empty);
                cache.Insert(KEY_EntityList, dt, new System.Web.Caching.CacheDependency(SystemListCacheWatchFile)
                    , System.Web.Caching.Cache.NoAbsoluteExpiration, new TimeSpan(0, minutesToCache, 0), System.Web.Caching.CacheItemPriority.AboveNormal, null);
            }
            return dt;
        }

        protected DataTable _GetCompanyList(bool ignoreCache = false)
        {
            var context = HttpContext.Current;
            var cache = context.Cache;
            string cacheKey = KEY_CompanyList + "_" + LUser.UsrId.ToString();
            int minutesToCache = companyListCacheMinutes; //1;
            Tuple<string, DataTable> dtCacheX = cache[cacheKey] as Tuple<string, DataTable>;
            if (dtCacheX == null || dtCacheX.Item1 != loginHandle || ignoreCache)
            {
                DataTable dtCompanyList = (new LoginSystem()).GetCompanyList(LImpr.Usrs, LImpr.RowAuthoritys, LImpr.Companys == "0" ? LImpr.Companys : LImpr.Companys);
                dtCacheX = new Tuple<string, DataTable>(loginHandle, dtCompanyList);
                cache.Insert(cacheKey, dtCacheX, new System.Web.Caching.CacheDependency(new string[] { SystemListCacheWatchFile }, new string[] { })
                    , System.Web.Caching.Cache.NoAbsoluteExpiration, new TimeSpan(0, minutesToCache, 0), System.Web.Caching.CacheItemPriority.AboveNormal, null);
            }
            return dtCacheX.Item2;
        }
        protected DataTable _GetProjectList(int companyId,bool ignoreCache=false)
        {
            var context = HttpContext.Current;
            var cache = context.Cache;
            string cacheKey = KEY_ProjectList + "_" +  companyId.ToString() + "_" + LUser.UsrId.ToString();
            int minutesToCache = projectListCacheMinutes; // 1;
            Tuple<string, DataTable> dtCacheX = cache[cacheKey] as Tuple<string, DataTable>;
            if (dtCacheX == null || dtCacheX.Item1 != loginHandle || ignoreCache)
            {
                DataTable dtProjectList = (new LoginSystem()).GetProjectList(LImpr.Usrs, LImpr.RowAuthoritys, LImpr.Projects, companyId.ToString());
                dtCacheX = new Tuple<string, DataTable>(loginHandle, dtProjectList);
                cache.Insert(cacheKey, dtCacheX, new System.Web.Caching.CacheDependency(new string[] { SystemListCacheWatchFile }, new string[] { })
                    , System.Web.Caching.Cache.NoAbsoluteExpiration, new TimeSpan(0, minutesToCache, 0), System.Web.Caching.CacheItemPriority.AboveNormal, null);
            }
            return dtCacheX.Item2;
        }
        protected bool ValidateScope(bool ignoreCache = false)
        {
            try
            {
                //DataTable dtMenu = _GetMenu(LCurr.SystemId, ignoreCache);
                //if (dtMenu.Rows.Count == 0) throw new Exception("access_denied");
                DataTable dtCompany = _GetCompanyList(ignoreCache);
                if (LCurr.CompanyId > 0 && LCurr.CompanyId != LUser.DefCompanyId && dtCompany.AsEnumerable().Where(dr => dr["CompanyId"].ToString() == LCurr.CompanyId.ToString()).Count() == 0)
                {
                    try
                    {
                        // force to first defined company if default not in list
                        LCurr.CompanyId = (int)dtCompany.AsEnumerable().Where(dr => !string.IsNullOrEmpty(dr["CompanyId"].ToString())).First()["CompanyId"];
                    }
                    catch
                    {
                        throw new UnauthorizedAccessException("access_denied");
                    }

                }
                DataTable dtProject = _GetProjectList(LCurr.CompanyId, ignoreCache);
                if (LCurr.ProjectId > 0 && LCurr.ProjectId != LUser.DefProjectId && dtProject.AsEnumerable().Where(dr => dr["ProjectId"].ToString() == LCurr.ProjectId.ToString()).Count() == 0)
                {
                    try
                    {
                        // force to first defined company if default not in list
                        LCurr.ProjectId = (int)dtProject.AsEnumerable().Where(dr => !string.IsNullOrEmpty(dr["ProjectId"].ToString())).First()["ProjectId"];
                    }
                    catch
                    {
                        throw new UnauthorizedAccessException("access_denied");
                    }
                }
                return true;
            }
            catch (Exception e)
            {
                return e == null;
            }
        }
        protected UsrImpr LoadUsrImpr(int usrId, byte systemId, int companyId, int projectId, bool ignoreCache)
        {
            string imprCacheKey = string.Format("{0}_{1}_{2}_{3}_{4}", KEY_CacheLImpr, usrId, systemId, companyId, projectId);
            var context = HttpContext.Current;
            var cache = context.Cache;
            var usrLoginHandle = loginHandle;
            Tuple<string, UsrImpr> imprX = cache[imprCacheKey] as Tuple<string, UsrImpr>;
            if (imprX == null || ignoreCache || usrLoginHandle != imprX.Item1)
            {
                int minutesToCache = usrImprCacheMinutes; // 1; // cache for 1 minute to avoid frequent DB retrieve for rapid firing API calls
                UsrImpr impr = SetImpersonation(null, usrId, systemId, companyId, projectId);
                imprX = new Tuple<string, UsrImpr>(usrLoginHandle, impr);
                cache.Insert(imprCacheKey, imprX,
                    new System.Web.Caching.CacheDependency(new string[] { SystemListCacheWatchFile },
                        usrLoginHandle == null ? new string[] { } : new string[] { usrLoginHandle })
                    , System.Web.Caching.Cache.NoAbsoluteExpiration, new TimeSpan(0, minutesToCache, 0), System.Web.Caching.CacheItemPriority.Default, null);
            }
            return imprX.Item2;
        }

        protected Dictionary<string, object> CreateUserSession(RintagiLoginToken token, byte? systemId = null, int? companyId = null, int? projectId = null)
        {
            Dictionary<string, string> dSys = GetSystemsDict()[systemId ?? token.SystemId];
            byte? dbId = systemId;
            LUser = new LoginUsr(token.LoginName, token.UsrId, token.UsrName, token.UsrEmail, "N", "N", 1, "English(US)", token.DefSystemId, token.DefProjectId, token.DefCompanyId, 9999, 0, false, null, null, false);
            LCurr = new UsrCurr(companyId ?? token.CompanyId, projectId ?? token.ProjectId, systemId ?? token.SystemId, dbId ?? token.DbId);
            LImpr = SetImpersonation(null, token.UsrId, systemId ?? token.SystemId, companyId ?? token.CompanyId, projectId ?? token.ProjectId);
            Dictionary<string, object> userSession = new Dictionary<string, object> { { "LUser", LUser }, { "LCurr", LCurr }, { "LImpr", LImpr } };
            return userSession;
        }

        protected void SetupAnonymousUser()
        {
            LUser = new LoginUsr("Anonymous", 1, "Anonymous", null, "N", "N", 1, "English(US)", 0, 0, 0, 9999, 0, false, null, null, false);
            LCurr = new UsrCurr(0, 0, 3, 3);
            LImpr = SetImpersonation(null, 1, LCurr.SystemId, LCurr.CompanyId, LCurr.ProjectId);
            loginHandle = "Anonymous";
        }

        protected void SwitchContext(byte sysId, int companyId, int projectId,bool checkSysId = true,bool checkCompanyId = true, bool checkProjectId = true, bool ignoreCache=false)
        {
            if (LcSystemId != sysId || (LCurr == null || LCurr.CompanyId != companyId || LCurr.ProjectId != projectId) || ignoreCache) {
                LcSystemId = sysId;
                LCurr.CompanyId = companyId;
                LCurr.ProjectId = projectId;
                LCurr.SystemId = sysId;
                LImpr = LoadUsrImpr(LUser.UsrId, sysId, companyId, projectId,ignoreCache);
                CPrj = new CurrPrj(((new RobotSystem()).GetEntityList()).Rows[0]);
                DataRow row = LoadSystemsList().Rows.Find(sysId);
                bool singleSQLCredential = (System.Configuration.ConfigurationManager.AppSettings["DesShareCred"] ?? "N") == "Y";
                string RedirectProjectRoot = System.Configuration.ConfigurationManager.AppSettings["RedirectProjectRoot"];

                CSrc = new CurrSrc(true, row);
                CTar = new CurrTar(true, row);
                if (singleSQLCredential)
                {
                    CPrj.SrcDesServer = Config.DesServer;
                    CPrj.SrcDesUserId = Config.DesUserId;
                    CPrj.SrcDesPassword = Config.DesPassword;
                    CPrj.TarDesServer = Config.DesServer;
                    CPrj.TarDesUserId = Config.DesUserId;
                    CPrj.TarDesPassword = Config.DesPassword;

                    CSrc.SrcServerName = Config.DesServer;
                    CSrc.SrcDbServer = Config.DesServer;
                    CSrc.SrcDbUserId = Config.DesUserId;
                    CSrc.SrcDbPassword = Config.DesPassword;

                    CTar.TarServerName = Config.DesServer;
                    CTar.TarDbServer = Config.DesServer;
                    CTar.TarDbUserId = Config.DesUserId;
                    CTar.TarDbPassword = Config.DesPassword;

                }
                if (!string.IsNullOrEmpty(RedirectProjectRoot))
                {
                    string[] redirect = RedirectProjectRoot.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToArray();
                    if (redirect.Length == 2)
                    {
                        CPrj.DeployPath = CPrj.DeployPath.Replace(redirect[0], redirect[1]);
                        CPrj.SrcClientProgramPath = CPrj.SrcClientProgramPath.Replace(redirect[0], redirect[1]);
                        CPrj.SrcRuleProgramPath = CPrj.SrcRuleProgramPath.Replace(redirect[0], redirect[1]);
                        CPrj.SrcWsProgramPath = CPrj.SrcWsProgramPath.Replace(redirect[0], redirect[1]);
                        CPrj.TarClientProgramPath = CPrj.TarClientProgramPath.Replace(redirect[0], redirect[1]);
                        CPrj.TarRuleProgramPath = CPrj.TarRuleProgramPath.Replace(redirect[0], redirect[1]);
                        CPrj.TarWsProgramPath = CPrj.TarWsProgramPath.Replace(redirect[0], redirect[1]);
                    }
                }

                Dictionary<string, string> sysDict = GetSystemsDict()[sysId];
                LcSysConnString = sysDict[KEY_SysConnectStr];
                LcAppConnString = sysDict[KEY_AppConnectStr];
                LcDesDb = sysDict[KEY_DesDb];
                LcAppDb = sysDict[KEY_AppDb];
                LcAppPw = sysDict[KEY_AppPwd];
            }
            if (checkSysId)
            {
                DataTable dtMenu = _GetMenu(sysId);
                if (dtMenu.Rows.Count == 0) throw new UnauthorizedAccessException("access_denied");
            }
            /* validate selected company/project */
            if (checkCompanyId && LCurr.CompanyId > 0)
            {
                DataTable dtCompany = _GetCompanyList();
                if (LCurr.CompanyId != LUser.DefCompanyId && dtCompany.AsEnumerable().Where(dr => dr["CompanyId"].ToString() == LCurr.CompanyId.ToString()).Count() == 0) throw new UnauthorizedAccessException("access_denied");
            }
            if (checkProjectId && LCurr.ProjectId > 0)
            {
                DataTable dtProject = _GetProjectList(LCurr.CompanyId);
                if (LCurr.ProjectId != LUser.DefProjectId && dtProject.AsEnumerable().Where(dr => dr["ProjectId"].ToString() == LCurr.ProjectId.ToString()).Count() == 0) throw new UnauthorizedAccessException("access_denied");
            }

        }
        protected void SwitchLang(short cultureId)
        {
            LUser.CultureId = cultureId;
        }


        protected Dictionary<string,object> LoadUserSession()
        {
            try
            {
                var context = HttpContext.Current;
                var cache = context.Cache;
//                var accessTokenInCookie = context.Request.Cookies["access_token"];
//                var refreshTokenInCookie = context.Request.Cookies["refresh_token"];
                var auth = (HttpContext.Current.Request.Headers["Authorization"] ?? HttpContext.Current.Request.Headers["X-Authorization"]) as string;
                var scope = HttpContext.Current.Request.Headers["X-RintagiScope"] as string;
                byte? systemId = null;
                int? companyId = null;
                int? projectId = null;
                short? cultureId = null;
                byte? dbId = null;
                bool hasSession = false;
                if (!string.IsNullOrEmpty(scope))
                {

                    var x = (scope??"").Split(new char[] { ',' });
                    try { systemId = byte.Parse(x[0]); dbId = systemId; } catch { };
                    try { companyId = int.Parse(x[1]); } catch { };
                    try { projectId = int.Parse(x[2]); } catch { };
                    try { cultureId = short.Parse(x[3]); } catch { };
                    try { dbId = byte.Parse(x[4]); } catch { };
                }
                if (hasSession && Session != null && Session[KEY_CacheLUser] != null)
                {
                    Dictionary<string, object> userSession = new Dictionary<string,object>(){{ "LUser", Session[KEY_CacheLUser] }, { "LCurr", Session[KEY_CacheLCurr] }, { "LImpr", Session[KEY_CacheLImpr] }};
                    LUser = userSession["LUser"] as LoginUsr;
                    LImpr = userSession["LImpr"] as UsrImpr;
                    LCurr = userSession["LCurr"] as UsrCurr;
                    return userSession;
                }
                else if (auth != null && auth.StartsWith("Bearer"))
                {
                    var x = auth.Split(new char[] { ' ' });
                    if (x.Length > 1)
                    {
                        var authObj = GetAuthObject();
                        RO.Facade3.RintagiLoginJWT token = authObj.GetLoginUsrInfo(x[1]);
                        var handle = token.handle;
                        var utc0 = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
                        var now = DateTime.Now.ToUniversalTime().Subtract(utc0).TotalSeconds;
                        var minutesToCache = usrHandleCacheMinutes; // 5;
                        var sessionEncryptionKey = authObj.GetSessionEncryptionKey(token.iat.ToString(), token.loginId.ToString());
                        var userSession = cache[handle] as Dictionary<string, object>;

                        if (now < token.exp && ValidateJWTHandle(handle)) {
                            RintagiLoginToken loginToken = authObj.DecryptLoginToken(token.loginToken, sessionEncryptionKey);
                            if ((
                                projectId != null && projectId.Value != loginToken.ProjectId
                                ) ||
                                (
                                companyId != null && companyId.Value != loginToken.CompanyId
                                ) ||
                                (
                                systemId != null && systemId.Value != loginToken.SystemId
                                ) ||
                                userSession == null
                                )
                            {
                                /* this means a browser refresh would remember the last scope for minutesToCache until token expiry */
                                userSession = CreateUserSession(loginToken, systemId, companyId, projectId);
                                cache.Insert(handle, userSession, new System.Web.Caching.CacheDependency(SystemListCacheWatchFile)
                                    , System.Web.Caching.Cache.NoAbsoluteExpiration, new TimeSpan(0, minutesToCache, 0), System.Web.Caching.CacheItemPriority.AboveNormal, null);
                            }

                            LUser = userSession["LUser"] as LoginUsr;
                            LImpr = userSession["LImpr"] as UsrImpr;
                            LCurr = userSession["LCurr"] as UsrCurr;
                            if (cultureId != null && LUser.CultureId != cultureId)
                            {
                                SwitchLang(cultureId.Value);
                            }
                            if (dbId != systemId && dbId != null)
                            {
                                //FIXME - must have this functionality to support screen generation etc.
                                //LCurr.DbId = dbId.Value;
                            }
                            loginHandle = handle;
                            return userSession;
                        }
                        else if (cache[handle] as Dictionary<string, object> != null)
                        {
                            cache.Remove(handle);
                        }
                    }
                }
            }
            catch (Exception e) {
                if (e.Message == "access_denied") throw;
            }
            return null;
        }
        protected ApiResponse<T,S> ProtectedCall<T,S>(Func<ApiResponse<T,S>> apiCallFn, bool allowAnonymous = false)
        {
            HttpContext Context = HttpContext.Current;
            try
            {
                Dictionary<string, object> userSessionInfo = LoadUserSession();

                if (!allowAnonymous && (LUser == null || LUser.UsrId == 1)) // not login or anonymous
                {
                    ApiResponse<T, S> mr = new ApiResponse<T, S>();
                    /* bypass browser prompt(like safari)
                    Context.Response.StatusCode = 401;
                     */
                    Context.Response.StatusCode = 200;
                    Context.Response.TrySkipIisCustomErrors = true;
                    mr.status = "access_denied";
                    mr.errorMsg = "requires login";
                    return mr;
                }
                else
                {
                    if (LUser == null) SetupAnonymousUser();
                    return ManagedApiCall(apiCallFn);
                }
            }
            catch (Exception e) {
                ApiResponse<T, S> mr = new ApiResponse<T, S>();
                /* bypass browser prompt(like safari)
                Context.Response.StatusCode = 401;
                 */
                Context.Response.StatusCode = 200;
                Context.Response.TrySkipIisCustomErrors = true;
                mr.status = "access_denied";
                mr.errorMsg = e.Message;
                return mr;
            }
        }
        protected async Task<ApiResponse<T, S>> ProtectedCallAsync<T, S>(Func<Task<ApiResponse<T, S>>> apiCallFn, bool allowAnonymous = false)
        {
            HttpContext Context = HttpContext.Current;
            try
            {
                Dictionary<string, object> userSessionInfo = LoadUserSession();

                if (!allowAnonymous && (LUser == null || LUser.UsrId == 1)) // not login or anonymous
                {
                    ApiResponse<T, S> mr = new ApiResponse<T, S>();
                    /* bypass browser prompt(like safari)
                    Context.Response.StatusCode = 401;
                     */
                    Context.Response.StatusCode = 200;
                    Context.Response.TrySkipIisCustomErrors = true;
                    mr.status = "access_denied";
                    mr.errorMsg = "requires login";
                    return mr;
                }
                else
                {
                    if (LUser == null) SetupAnonymousUser();
                    return await ManagedApiCallAsync(apiCallFn);
                }
            }
            catch (Exception e)
            {
                ApiResponse<T, S> mr = new ApiResponse<T, S>();
                /* bypass browser prompt(like safari)
                Context.Response.StatusCode = 401;
                 */
                Context.Response.StatusCode = 200;
                Context.Response.TrySkipIisCustomErrors = true;
                mr.status = "access_denied";
                mr.errorMsg = e.Message;
                return mr;
            }
        }
        protected Action<Exception> GetErrorTracing()
        {
            var Context = HttpContext.Current;
            return (e) =>
            {
                GetErrorTracingEx(Context != null ? Context.Request : null)(e, null);
            };
        }
        protected Action<Exception, string> GetErrorTracingEx(HttpRequest Request)
        {
            return (e, severity) =>
            {
                string supportEmail = System.Configuration.ConfigurationManager.AppSettings["TechSuppEmail"];
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
                        var request = Request;
                        string xForwardedFor = request != null ? request.Headers["X-Forwarded-For"] : null;
                        string xForwardedHost = request != null ? request.Headers["X-Forwarded-Host"] : null;
                        string xForwardedProto = request != null ? request.Headers["X-Forwarded-Proto"] : null;
                        string xOriginalURL = request != null ? request.Headers["X-Original-URL"] : null;

                        string sourceIP = string.Format("From: {0} Forwarded for: {1} \r\n\r\n", Request != null ? Request.UserHostAddress : "unknown source ip", xForwardedFor);
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
                        mm.Subject = webtitle + " Application Error " + (Request != null ? Request.Url.GetLeftPart(UriPartial.Path) : "unknown request url");
                        mm.Body = (Request != null ? Request.Url.ToString() : "unknown request url" ) 
                                + "\r\n\r\n" 
                                + sourceIP 
                                + usrId 
                                + machine 
                                + currentTime 
                                + roVersion
                                + exMessages[exMessages.Count - 1] + "\r\n\r\n" + e.StackTrace + (innerException != null ? "\r\n InnerException: \r\n\r\n" + string.Join("\r\n", exMessages.ToArray()) + "\r\n\r\n" + innerException.StackTrace : "") + "\r\n";
                        mm.IsBodyHtml = false;
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
            };
        }

        protected void ErrorTracing(Exception e)
        {
            GetErrorTracingEx(HttpContext.Current != null ? HttpContext.Current.Request : null)(e, null);
        }
        protected void ErrorTrace(Exception e, string severity)
        {
            GetErrorTracingEx(HttpContext.Current != null ? HttpContext.Current.Request : null)(e, severity);
        }
        protected ApiResponse<T,S> ManagedApiCall<T,S>(Func<ApiResponse<T,S>> apiCallFn)
        {
            try 
            {
                return apiCallFn();
            }
            catch (Exception e)
            {
                string supportEmail = System.Configuration.ConfigurationManager.AppSettings["TechSuppEmail"];
                if (supportEmail != "none" && supportEmail != string.Empty)
                {
                    try
                    {
                        HttpRequest Request = HttpContext.Current.Request;
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
                        string machine = string.Format("Machine: {0}\r\n\r\n", Environment.MachineName);
                        System.Net.Mail.MailMessage mm = new System.Net.Mail.MailMessage();
                        string[] receipients = to.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                        var request = Request;
                        string xForwardedFor = request != null ? request.Headers["X-Forwarded-For"] : null;
                        string xForwardedHost = request != null ? request.Headers["X-Forwarded-Host"] : null;
                        string xForwardedProto = request != null ? request.Headers["X-Forwarded-Proto"] : null;
                        string xOriginalURL = request != null ? request.Headers["X-Original-URL"] : null;
                        string sourceIP = string.Format("From: {0}, ForwardedFor: {1}\r\n\r\n",Request.UserHostAddress, xForwardedFor);
                        string usrId = string.Format("User: {0}\r\n\r\n", LUser != null ? LUser.UsrId.ToString() : "");
                        string currentTime = string.Format("Server Time: {0} \r\n\r\n UTC: {1} \r\n\r\n", DateTime.Now.ToString("O"), DateTime.UtcNow.ToString("O"));
                        string roVersion = string.Format("RO Version: {0}\r\n\r\n", ROVersion);
                        Exception innerException = e.InnerException;

                        foreach (var t in receipients)
                        {
                            mm.To.Add(new System.Net.Mail.MailAddress(t.Trim()));
                        }
                        mm.Subject = webtitle + " Application Error " + Request.Url.GetLeftPart(UriPartial.Path);
                        mm.Body = Request.Url.ToString() 
                                + "\r\n\r\n" 
                                + sourceIP 
                                + usrId
                                + machine
                                + currentTime
                                + roVersion
                                + e.Message + "\r\n\r\n" + e.StackTrace + (innerException != null ? "\r\n InnerException: \r\n\r\n" + innerException.Message + "\r\n\r\n" + innerException.StackTrace : "") + "\r\n";
                        mm.IsBodyHtml = false;
                        mm.From = new System.Net.Mail.MailAddress(string.IsNullOrEmpty(username) || !(username ?? "").Contains("@") ? from : username, string.IsNullOrEmpty(fromTitle) ? from : fromTitle);    // Address must be the same as the smtp login user.
                        mm.ReplyToList.Add(new System.Net.Mail.MailAddress(string.IsNullOrEmpty(replyTo) ? from : replyTo)); // supplied from would become reply too for the 'sending on behalf of'
                        (new RO.WebRules.WebRule()).SendEmail(bSsl, port, server, username, password, domain, mm);
                        mm.Dispose();   // Error is trapped and reported from the caller.

                    }
                    catch (Exception ex)
                    {
                        // never happen, i.e. do nothing just to get around unnecessary compilation warning
                        if (ex == null) return null;
                    }
                }
                return new ApiResponse<T,S>() { 
                    errorMsg = e.Message + (Config.DeployType=="DEV" ? e.StackTrace : "")
                    ,status = e is UnauthorizedAccessException ? "unauthorized_access" : "failed"
                };
            }
        }

        protected async Task<ApiResponse<T, S>> ManagedApiCallAsync<T, S>(Func<Task<ApiResponse<T, S>>> apiCallFn)
        {
            try
            {
                return await apiCallFn();
            }
            catch (Exception e)
            {
                string supportEmail = System.Configuration.ConfigurationManager.AppSettings["TechSuppEmail"];
                if (supportEmail != "none" && supportEmail != string.Empty)
                {
                    try
                    {
                        HttpRequest Request = HttpContext.Current.Request;
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
                        string sourceIP = string.Format("From: {0}\r\n\r\n", Request.UserHostAddress);
                        string usrId = string.Format("User: {0}\r\n\r\n", LUser != null ? LUser.UsrId.ToString() : "");
                        Exception innerException = e.InnerException;

                        foreach (var t in receipients)
                        {
                            mm.To.Add(new System.Net.Mail.MailAddress(t.Trim()));
                        }
                        mm.Subject = webtitle + " Application Error " + Request.Url.GetLeftPart(UriPartial.Path);
                        mm.Body = Request.Url.ToString() + "\r\n\r\n" + sourceIP + usrId + e.Message + "\r\n\r\n" + e.StackTrace + (innerException != null ? "\r\n InnerException: \r\n\r\n" + innerException.Message + "\r\n\r\n" + innerException.StackTrace : "") + "\r\n";
                        mm.IsBodyHtml = false;
                        mm.From = new System.Net.Mail.MailAddress(string.IsNullOrEmpty(username) || !(username ?? "").Contains("@") ? from : username, string.IsNullOrEmpty(fromTitle) ? from : fromTitle);    // Address must be the same as the smtp login user.
                        mm.ReplyToList.Add(new System.Net.Mail.MailAddress(string.IsNullOrEmpty(replyTo) ? from : replyTo)); // supplied from would become reply too for the 'sending on behalf of'
                        (new RO.WebRules.WebRule()).SendEmail(bSsl, port, server, username, password, domain, mm);
                        mm.Dispose();   // Error is trapped and reported from the caller.

                    }
                    catch (Exception ex)
                    {
                        // never happen, i.e. do nothing just to get around unnecessary compilation warning
                        if (ex == null) return null;
                    }
                }
                return new ApiResponse<T, S>()
                {
                    errorMsg = e.Message + (Config.DeployType == "DEV" ? e.StackTrace : "")
                    ,
                    status = e is UnauthorizedAccessException ? "unauthorized_access" : "failed"
                };
            }
        }

        protected Func<ApiResponse<T, S>> RestrictedApiCall<T, S>(Func<ApiResponse<T, S>> apiCallFn, byte systemId, int screenId, string action, string columnName, Func<ApiResponse<T, S>> OnErrorResponse = null)
        {
            Func<ApiResponse<T,S>> fn = () =>
            {
                SwitchContext(GetSystemId(), LCurr.CompanyId, LCurr.ProjectId);
                Func<ApiResponse<T, S>> errRetFn = () =>
                {
                    if (OnErrorResponse != null) return OnErrorResponse();

                    ApiResponse<T, S> mr = new ApiResponse<T, S>();
                    /* bypass browser prompt(like safari)
                    Context.Response.StatusCode = 401;
                     */
                    Context.Response.StatusCode = 200;
                    Context.Response.TrySkipIisCustomErrors = true;
                    mr.status = "access_denied";
                    mr.errorMsg = "access denied";
                    return mr;
                };
                if (screenId > 0 || !string.IsNullOrEmpty(columnName))
                {
                    Dictionary<string, DataRow> dtMenuAccess = GetScreenMenu(systemId, screenId);
                    DataTable dtAuthRow = _GetAuthRow(screenId);
                    DataTable dtAuthCol = _GetAuthCol(screenId);
                    Dictionary<string, DataRow> authCol = dtAuthCol.AsEnumerable().ToDictionary(dr => dr["ColName"].ToString());
                    
                    if ( // screen based checking, i.e. record level
                        !AllowAnonymous() &&
                        ((dtMenuAccess != null && !dtMenuAccess.ContainsKey(screenId.ToString()))
                        || dtMenuAccess == null
                        || dtAuthRow.Rows.Count == 0
                        || (dtAuthRow.Rows[0]["ViewOnly"].ToString() == "Y" && (action == "S" || action == "A" || action=="U" || action == "D"))
                        || (dtAuthRow.Rows[0]["AllowAdd"].ToString() == "N" && dtAuthRow.Rows[0]["AllowUpd"].ToString() == "N" && action == "S")
                        || (dtAuthRow.Rows[0]["AllowAdd"].ToString() == "N" && action == "A")
                        || (dtAuthRow.Rows[0]["AllowUpd"].ToString() == "N" && action == "U")
                        || (dtAuthRow.Rows[0]["AllowDel"].ToString() == "N" && action == "D")
                        ))
                    {
                        return errRetFn();
                        //ApiResponse<T, S> mr = new ApiResponse<T, S>();
                        //Context.Response.StatusCode = 401;
                        //Context.Response.TrySkipIisCustomErrors = true;
                        //mr.status = "access_denied";
                        //mr.errorMsg = "access denied";
                        //return mr;
                    }
                    else if (!string.IsNullOrEmpty(columnName) &&
                            ((authCol.ContainsKey(columnName) && (authCol[columnName]["ColVisible"].ToString() == "N")) || (!authCol.ContainsKey(columnName) && !authCol.ContainsKey(columnName+"Text"))))
                    {
                        return errRetFn();

                        //ApiResponse<T, S> mr = new ApiResponse<T, S>();
                        //Context.Response.StatusCode = 401;
                        //Context.Response.TrySkipIisCustomErrors = true;
                        //mr.status = "access_denied";
                        //mr.errorMsg = "access denied";
                        //return mr;
                    }
                }
                return apiCallFn();
            };

            return fn;

        }

        protected Func<Task<ApiResponse<T, S>>> RestrictedApiCallAsync<T, S>(Func<Task<ApiResponse<T, S>>> apiCallFn, byte systemId, int screenId, string action, string columnName, Func<Task<ApiResponse<T, S>>> OnErrorResponse = null)
        {
            Func<Task<ApiResponse<T, S>>> fn = async () =>
            {
                SwitchContext(GetSystemId(), LCurr.CompanyId, LCurr.ProjectId);
                Func<Task<ApiResponse<T, S>>> errRetFn = async () =>
                {
                    if (OnErrorResponse != null) return await OnErrorResponse();

                    ApiResponse<T, S> mr = new ApiResponse<T, S>();
                    /* bypass browser prompt(like safari)
                    Context.Response.StatusCode = 401;
                     */
                    Context.Response.StatusCode = 200;
                    Context.Response.TrySkipIisCustomErrors = true;
                    mr.status = "access_denied";
                    mr.errorMsg = "access denied";
                    return mr;
                };
                if (screenId > 0 || !string.IsNullOrEmpty(columnName))
                {
                    Dictionary<string, DataRow> dtMenuAccess = GetScreenMenu(systemId, screenId);
                    DataTable dtAuthRow = _GetAuthRow(screenId);
                    DataTable dtAuthCol = _GetAuthCol(screenId);
                    Dictionary<string, DataRow> authCol = dtAuthCol.AsEnumerable().ToDictionary(dr => dr["ColName"].ToString());

                    if ( // screen based checking, i.e. record level
                        !AllowAnonymous() &&
                        ((dtMenuAccess != null && !dtMenuAccess.ContainsKey(screenId.ToString()))
                        || dtMenuAccess == null
                        || dtAuthRow.Rows.Count == 0
                        || (dtAuthRow.Rows[0]["ViewOnly"].ToString() == "Y" && (action == "S" || action == "D"))
                        || (dtAuthRow.Rows[0]["AllowAdd"].ToString() == "N" && dtAuthRow.Rows[0]["AllowUpd"].ToString() == "N" && action == "S")
                        || (dtAuthRow.Rows[0]["AllowDel"].ToString() == "N" && action == "D")
                        ))
                    {
                        return await errRetFn();
                        //ApiResponse<T, S> mr = new ApiResponse<T, S>();
                        //Context.Response.StatusCode = 401;
                        //Context.Response.TrySkipIisCustomErrors = true;
                        //mr.status = "access_denied";
                        //mr.errorMsg = "access denied";
                        //return mr;
                    }
                    else if (!string.IsNullOrEmpty(columnName) &&
                            ((authCol.ContainsKey(columnName) && (authCol[columnName]["ColVisible"].ToString() == "N")) || (!authCol.ContainsKey(columnName) && !authCol.ContainsKey(columnName + "Text"))))
                    {
                        return await errRetFn();

                        //ApiResponse<T, S> mr = new ApiResponse<T, S>();
                        //Context.Response.StatusCode = 401;
                        //Context.Response.TrySkipIisCustomErrors = true;
                        //mr.status = "access_denied";
                        //mr.errorMsg = "access denied";
                        //return mr;
                    }
                }
                return await apiCallFn();
            };

            return fn;

        }

        protected bool _AllowScreenColumnAccess(int screenId, string columnName, string action, Dictionary<string, DataRow> dtMenuAccess, DataTable dtAuthRow, Dictionary<string, DataRow> authCol)
        {
            if ( // screen based checking, i.e. record level
                !AllowAnonymous() &&
                ((dtMenuAccess != null && !dtMenuAccess.ContainsKey(screenId.ToString()))
                || dtMenuAccess == null
                || dtAuthRow.Rows.Count == 0
                || (dtAuthRow.Rows[0]["ViewOnly"].ToString() == "Y" && (action == "S" || action == "D"))
                || (dtAuthRow.Rows[0]["AllowAdd"].ToString() == "N" && dtAuthRow.Rows[0]["AllowUpd"].ToString() == "N" && action == "S")
                || (dtAuthRow.Rows[0]["AllowDel"].ToString() == "N" && action == "D")
                ))
            {
                return false;
            }

            else if (!string.IsNullOrEmpty(columnName) &&
                    ((authCol.ContainsKey(columnName) && (authCol[columnName]["ColVisible"].ToString() == "N")) || (!authCol.ContainsKey(columnName) && !authCol.ContainsKey(columnName + "Text"))))
            {
                return false;
            }

            return true;
        }

        protected DataTable _GetLabels(string labelCat)
        {
            var context = HttpContext.Current;
            var cache = context.Cache;
            string cacheKey = loginHandle + "_SystemLabels_" + GetSystemId() + "_" + LUser.CultureId.ToString() + "_" + labelCat.ToString();
            int minutesToCache = systemLabelCacheMinutes; // 1;
            DataTable dtLabel = cache[cacheKey] as DataTable;
            if (dtLabel == null)
            {
                /* this should be fixed in GetLabels in SP as it doesn't support default FIXEME */
                //dtLabel = (new AdminSystem()).GetLabels(LUser.CultureId, labelCat, LCurr.CompanyId.ToString(), LcSysConnString, LcAppPw);
                dtLabel = (new AdminSystem()).GetLabels(LUser.CultureId, labelCat, null, LcSysConnString, LcAppPw);
                cache.Add(cacheKey, dtLabel, new System.Web.Caching.CacheDependency(new string[] { }, loginHandle == null ? new string[] {} : new string[] { loginHandle })
                    , System.Web.Caching.Cache.NoAbsoluteExpiration, new TimeSpan(0, minutesToCache, 0), System.Web.Caching.CacheItemPriority.AboveNormal, null);
            }
            return dtLabel;
        }

        protected string _GetLabel(string labelCat, string labelKey)
        {
            var context = HttpContext.Current;
            var cache = context.Cache;
            string cacheKey = loginHandle + "_SystemLabel_" + GetSystemId().ToString() + "_" + LUser.CultureId.ToString() + "_" + labelCat.ToString() + "_" + labelKey;
            int minutesToCache = 1;
            string label = cache[cacheKey] as string;
            if (label == null)
            {
                label = (new AdminSystem()).GetLabel(LUser != null && LUser.LoginName.ToLower() != "anonymous" ? LUser.CultureId : (short)1, labelCat, labelKey, LCurr.CompanyId.ToString(), LcSysConnString, LcAppPw);
                cache.Add(cacheKey, label, new System.Web.Caching.CacheDependency(new string[] { }, loginHandle == null ? new string[] { } : new string[] { loginHandle })
                    , System.Web.Caching.Cache.NoAbsoluteExpiration, new TimeSpan(0, minutesToCache, 0), System.Web.Caching.CacheItemPriority.AboveNormal, null);
            }
            return label;
        }

        protected DataTable _GetScreenButtonHlp(int screenId)
        {
            var context = HttpContext.Current;
            var cache = context.Cache;
            string cacheKey = loginHandle + "_ScreenButtonHlp_" + GetSystemId().ToString() + "_" + LUser.CultureId.ToString() + "_" + screenId.ToString();
            DataTable dtButtonHlp = cache[cacheKey] as DataTable;
            int minutesToCache = screenButtonCacheMinutes; //1;
            if (dtButtonHlp == null)
            {
                dtButtonHlp = (new AdminSystem()).GetButtonHlp(GetScreenId(), 0, 0, LUser.CultureId, LcSysConnString, LcAppPw);
                cache.Add(cacheKey, dtButtonHlp, new System.Web.Caching.CacheDependency(new string[] { }, loginHandle == null ? new string[] { } : new string[] { loginHandle })
                    , System.Web.Caching.Cache.NoAbsoluteExpiration, new TimeSpan(0, minutesToCache, 0), System.Web.Caching.CacheItemPriority.AboveNormal, null);
            }
            return dtButtonHlp;
        }
        protected DataTable _GetScrCriteria(int screenId)
        {
            var context = HttpContext.Current;
            var cache = context.Cache;
            string cacheKey = loginHandle + "_ScrCriteria_" + GetSystemId().ToString() + "_" + LCurr.CompanyId.ToString() + "_" + LCurr.ProjectId.ToString() + "_" + screenId.ToString();
            int minutesToCache = screenCriteriaCacheMinutes; // 1;
            DataTable dtScreenCriteria = null;
            lock (cache)
            {
                DataTable dt = cache[cacheKey] as DataTable;
                if (dt != null) dtScreenCriteria = dt.Copy();
            }

            if (dtScreenCriteria == null)
            {
                dtScreenCriteria = (new AdminSystem()).GetScrCriteria(screenId.ToString(), LcSysConnString, LcAppPw);
                lock (cache)
                {
                    if (cache[cacheKey] as DataTable == null)
                    {
                        cache.Insert(cacheKey, dtScreenCriteria, new System.Web.Caching.CacheDependency(new string[] { }, loginHandle == null ? new string[] { } : new string[] { loginHandle })
                            , System.Web.Caching.Cache.NoAbsoluteExpiration, new TimeSpan(0, minutesToCache, 0), System.Web.Caching.CacheItemPriority.AboveNormal, null);
                    }
                }
            }
            return dtScreenCriteria;
        }

        protected Tuple<List<string>, SerializableDictionary<string, string>> _GetCurrentScrCriteria(DataView dv, SerializableDictionary<string, SerializableDictionary<string, string>> lastCriteria, bool useLast)
        {
            List<string> currentCriteriaList = dv.Count == 0 ? new List<string>() : new List<string>(Enumerable.Range(0, dv.Count).Select(_ => ""));
            SerializableDictionary<string, string> currentScrCriteria = new SerializableDictionary<string, string>();
            int ii = 0;
            foreach (DataRowView drv in dv)
            {
                bool required = drv["RequiredValid"].ToString() == "Y";
                string columnName = drv["ColumnName"].ToString();
                string keyName = drv["DdlKeyColumnName"].ToString();
                string lastValue =lastCriteria[columnName] != null ? lastCriteria[columnName]["LastCriteria"] : "";
                if (!required)
                {
                    currentCriteriaList[ii] = currentScrCriteria[columnName] = lastValue;
                }
                else if (drv["DisplayMode"].ToString() == "AutoListBox")
                {
                    // FIXME, keylist needs to be validated properly for required criteria
                    currentCriteriaList[ii] = lastValue;
                }
                else if (drv["DisplayName"].ToString() == "ComboBox" ||
                    drv["DisplayName"].ToString() == "DropDownList" ||
                    drv["DisplayName"].ToString() == "ListBox" ||
                    drv["DisplayMode"].ToString() == "AutoListBox" ||
                    drv["DisplayName"].ToString() == "RadioButtonList"
                    )
                {
                    var list = GetScreenCriteriaDdlList(drv["ScreenCriId"].ToString(), !string.IsNullOrEmpty(lastValue) ? "**" + lastValue : "", 0, "");
                    try
                    {
                        var firstChoice = list.data.data.FirstOrDefault();
                        var lastValid = list.data.data.Where(v => v["key"] == lastValue).Count() > 0;
                        currentCriteriaList[ii] = currentScrCriteria[columnName] = 
                                                            useLast && lastValid && !string.IsNullOrEmpty(lastValue)
                                                            ? lastValue
                                                            : (firstChoice != null) ? firstChoice["key"] : MakeCriteriaVal(drv.Row, "");
                    }
                    catch (Exception ex)
                    {
                        // invalid value or something else 
                        currentCriteriaList[ii] = MakeCriteriaVal(drv.Row, "");
                        // never happen, i.e. do nothing just to get around unnecessary compilation warning
                        if (ex == null) return null;
                    }

                }
                else currentCriteriaList[ii] = currentScrCriteria[columnName] =
                                                                required ? (useLast && !string.IsNullOrEmpty(lastValue) ? lastValue : MakeCriteriaVal(drv.Row, ""))
                                                                         : (useLast ? lastValue : MakeCriteriaVal(drv.Row, ""));
                ii = ii + 1;
            }
            return new Tuple<List<string>, SerializableDictionary<string, string>>(currentCriteriaList, currentScrCriteria);
        }

        protected List<string> _SetCurrentScrCriteria(List<string> criteria)
        {
            return _CurrentScreenCriteria = criteria;
        }

        protected DataTable _GetScreenCriHlp(int screenId)
        {
            var context = HttpContext.Current;
            var cache = context.Cache;
            string cacheKey = loginHandle + "_ScreenCriHlp_" + GetSystemId().ToString() + "_" + LUser.CultureId.ToString() + "_" + screenId.ToString();
            int minutesToCache = screenCriHlpCacheMinutes;// 1;
            DataTable dtCriHlp = cache[cacheKey] as DataTable;
            if (dtCriHlp == null)
            {
                dtCriHlp = (new AdminSystem()).GetScreenCriHlp(screenId, LUser.CultureId, LcSysConnString, LcAppPw);
                cache.Add(cacheKey, dtCriHlp, new System.Web.Caching.CacheDependency(new string[] { }, loginHandle == null ? new string[] { } : new string[] { loginHandle })
                    , System.Web.Caching.Cache.NoAbsoluteExpiration, new TimeSpan(0, minutesToCache, 0), System.Web.Caching.CacheItemPriority.AboveNormal, null);
            }
            return dtCriHlp;
        }

        protected void _MkScreenIn(int screenId, string screenCriId, string sp, string multiDesign, bool refresh)
        {
            var context = HttpContext.Current;
            var cache = context.Cache;
            string cacheKey = "_ScreenInCri_" + GetSystemId().ToString() + "_" + screenId.ToString() + "_" + screenCriId;
            int minutesToCache = screenInCriCacheMinutes; // 60;
            if (cache[cacheKey] == null || refresh)
            {
                try
                {
                    /* this SHOULD NOT BE DONE ON ACCESS BUT GenScreen, the whole design of these Cri SP creation is WRONG !*/
                    //(new AdminSystem()).MkGetScreenIn(screenId.ToString(), screenCriId.ToString(), sp, LcAppDb, LcDesDb, multiDesign, LcSysConnString, LcAppPw,Config.DeployType == "DEV");
                    (new AdminSystem()).MkGetScreenIn(screenId.ToString(), screenCriId.ToString(), sp, LcAppDb, LcDesDb, multiDesign, LcSysConnString, LcAppPw,true);
                    cache.Add(cacheKey, DateTime.UtcNow, new System.Web.Caching.CacheDependency(new string[] { }, loginHandle == null ? new string[] { } : new string[] { loginHandle })
                        , System.Web.Caching.Cache.NoAbsoluteExpiration, new TimeSpan(0, minutesToCache, 0), System.Web.Caching.CacheItemPriority.AboveNormal, null);

                }
                catch
                {
                    // can have race condition too !
                }
                // The MkGetScreenIn do a DROP then CREATE which can have time gap before it is being SEEN !!
                // fixed in SP in MkGetScreenIn as CREATE IF NOT EXISTS
                // System.Threading.Thread.Sleep(10);
            }

        }
        protected void _MkScreenIn(int screenId, DataRowView drv, bool refresh)
        {
            _MkScreenIn(screenId, drv["ScreenCriId"].ToString(), "GetDdl" + drv["ColumnName"].ToString() + GetSystemId().ToString() + "C" + drv["ScreenCriId"].ToString(), drv["MultiDesignDb"].ToString(), refresh);
        }

        protected DataTable _GetScreenIn(int screenId, DataRowView drv, bool refresh)
        {
            var context = HttpContext.Current;
            var cache = context.Cache;
            string cacheKey = loginHandle + "_ScreenIn_" + GetSystemId().ToString() + "_" + LCurr.CompanyId.ToString() + "_" + LCurr.ProjectId.ToString() + "_" + screenId.ToString() + "_" + drv["ScreenCriId"].ToString();
            int minutesToCache = screenInCacheMinutes; // 1;
            Tuple<string, DataTable> dtCacheX = cache[cacheKey] as Tuple<string, DataTable>;

            if (dtCacheX == null || dtCacheX.Item1 != loginHandle || refresh)
            {
                _MkScreenIn(screenId, drv, refresh);
                try
                {
                    DataTable dtScreenIn = (new AdminSystem()).GetScreenIn(screenId.ToString(), "GetDdl" + drv["ColumnName"].ToString() + GetSystemId().ToString() + "C" + drv["ScreenCriId"].ToString(), 0, drv["RequiredValid"].ToString(), 0, string.Empty, true, string.Empty, LImpr, LCurr, drv["MultiDesignDb"].ToString() == "N" ? LcAppConnString : LcSysConnString, LcAppPw);
                    dtCacheX = new Tuple<string, DataTable>(loginHandle, dtScreenIn);
                    cache.Insert(cacheKey, dtCacheX, new System.Web.Caching.CacheDependency(new string[] { }, loginHandle == null ? new string[] { } : new string[] { loginHandle })
                        , System.Web.Caching.Cache.NoAbsoluteExpiration, new TimeSpan(0, minutesToCache, 0), System.Web.Caching.CacheItemPriority.AboveNormal, null);
                }
                catch (Exception ex)
                {
                    ErrorTracing(new Exception(string.Format("{0} {1}", LcAppConnString, GetSystemId()), ex));
                    throw;
                }
            }
            return dtCacheX.Item2;
        }

        protected DataTable _GetScreenTab(int screenId)
        {
            var context = HttpContext.Current;
            var cache = context.Cache;
            string cacheKey = loginHandle + "_ScreenTab_" + GetSystemId().ToString() + "_" + LCurr.CompanyId.ToString() + "_" + LCurr.ProjectId.ToString() + "_" + screenId.ToString();
            int minutesToCache = screenTabCacheMinutes; // 1;

            DataTable dtScreenTab = cache[cacheKey] as DataTable;
            if (dtScreenTab == null)
            {
                dtScreenTab = (new AdminSystem()).GetScreenTab(screenId, LUser.CultureId, LcSysConnString, LcAppPw);
                cache.Add(cacheKey, dtScreenTab, new System.Web.Caching.CacheDependency(new string[] { }, loginHandle == null ? new string[] { } : new string[] { loginHandle })
                    , System.Web.Caching.Cache.NoAbsoluteExpiration, new TimeSpan(0, minutesToCache, 0), System.Web.Caching.CacheItemPriority.AboveNormal, null);
            }
            return dtScreenTab;
        }

        protected DataTable _GetScreenHlp(int screenId)
        {
            var context = HttpContext.Current;
            var cache = context.Cache;
            string cacheKey = loginHandle + "_ScreenHelp_" + GetSystemId().ToString() + "_" + LUser.CultureId.ToString()  + "_" + screenId.ToString();
            int minutesToCache = screenHlpCacheMinutes; // 1;

            DataTable dtScreenHlp = cache[cacheKey] as DataTable;
            if (dtScreenHlp == null)
            {
                dtScreenHlp = (new AdminSystem()).GetScreenHlp(screenId, LUser.CultureId, LcSysConnString, LcAppPw);
                cache.Add(cacheKey, dtScreenHlp, new System.Web.Caching.CacheDependency(new string[] { }, loginHandle == null ? new string[] { } : new string[] { loginHandle })
                    , System.Web.Caching.Cache.NoAbsoluteExpiration, new TimeSpan(0, minutesToCache, 0), System.Web.Caching.CacheItemPriority.AboveNormal, null);
            }
            return dtScreenHlp;
        }

        protected DataTable _GetLastScrCriteria(int screenId,int rowExpected, bool refresh=false)
        {
            var context = HttpContext.Current;
            var cache = context.Cache;
            string cacheKey = loginHandle + "_ScreenLastCriteria_" + GetSystemId().ToString() + "_" + screenId.ToString();
            int minutesToCache = screenLstCriCacheMinutes;// 1;
            Tuple<string, DataTable> dtCacheX = cache[cacheKey] as Tuple<string, DataTable>;
            if (dtCacheX == null || dtCacheX.Item1 != loginHandle || refresh)
            {
                DataTable dtLastCriteria = null;
                bool isCacheable = false;
                try
                {
                    dtLastCriteria = (new AdminSystem()).GetLastCriteria(rowExpected, screenId, 0, LUser.UsrId, LcSysConnString, LcAppPw);
                    isCacheable = true;
                }
                catch (Exception ex)
                {
                    ErrorTracing(new Exception(string.Format("GetLastScriteria error, SystemId:{0} ScreenId:{1}", GetSystemId(), screenId), ex));
                    // treated as no prior saved value, this is not a critcal error and should not be shown to end user, the GetLastCriteria SP 
                    // needs to be reviewed and explain why it throw an error
                    dtLastCriteria = new DataTable();
                    dtLastCriteria.Columns.Add(new DataColumn("LastCriteria", typeof(string)));
                }
                dtCacheX = new Tuple<string, DataTable>(loginHandle, dtLastCriteria);
                if (isCacheable)
                {
                    cache.Insert(cacheKey, dtCacheX, new System.Web.Caching.CacheDependency(new string[] { }, loginHandle == null ? new string[] { } : new string[] { loginHandle })
                        , System.Web.Caching.Cache.NoAbsoluteExpiration, new TimeSpan(0, minutesToCache, 0), System.Web.Caching.CacheItemPriority.AboveNormal, null);
                }
            }
            return dtCacheX.Item2;
        }
        protected DataTable _GetScreenFilter(int screenId, bool refresh=false)
        {
            var context = HttpContext.Current;
            var cache = context.Cache;
            string cacheKey = loginHandle + "_ScreenFilter_" + GetSystemId().ToString() + "_" + LUser.CultureId.ToString() + "_" + LCurr.CompanyId.ToString() + "_" + LCurr.ProjectId.ToString() + "_" + screenId.ToString();
            int minutesToCache = screenFilterCacheMinutes; // 1;
            DataTable dtScreenFilter = cache[cacheKey] as DataTable;
            if (dtScreenFilter == null)
            {
                dtScreenFilter = (new AdminSystem()).GetScreenFilter(screenId, LUser.CultureId, LcSysConnString, LcAppPw);
                cache.Insert(cacheKey, dtScreenFilter, new System.Web.Caching.CacheDependency(new string[] { }, loginHandle == null ? new string[] { } : new string[] { loginHandle })
                    , System.Web.Caching.Cache.NoAbsoluteExpiration, new TimeSpan(0, minutesToCache, 0), System.Web.Caching.CacheItemPriority.AboveNormal, null);
            }
            return dtScreenFilter;
        }
        protected DataTable _GetAuthRow(int screenId)
        {
            var context = HttpContext.Current;
            var cache = context.Cache;
            string cacheKey = loginHandle + "_ScreenAutRow_" + GetSystemId().ToString() + "_" + LCurr.CompanyId.ToString() + "_" + LCurr.ProjectId.ToString() + "_" + screenId.ToString();
            int minutesToCache = screenAuthRowCacheMinutes; // 1;
            Tuple<string, DataTable> dtCacheX = cache[cacheKey] as Tuple<string, DataTable>;
            if (dtCacheX == null || dtCacheX.Item1 != loginHandle)
            {
                DataTable dtAuthRow = (new AdminSystem()).GetAuthRow(screenId, LImpr.RowAuthoritys, LcSysConnString, LcAppPw);
                dtCacheX = new Tuple<string, DataTable>(loginHandle, dtAuthRow);
                cache.Insert(cacheKey, dtCacheX, new System.Web.Caching.CacheDependency(new string[] { }, loginHandle == null ? new string[] { } : new string[] { loginHandle })
                    , System.Web.Caching.Cache.NoAbsoluteExpiration, new TimeSpan(0, minutesToCache, 0), System.Web.Caching.CacheItemPriority.AboveNormal, null);
            }
            return dtCacheX.Item2;
        }
        protected DataTable _GetAuthCol(int screenId)
        {
            var context = HttpContext.Current;
            var cache = context.Cache;
            string cacheKey = loginHandle + "_ScreenAutCol_" + GetSystemId().ToString() + "_" + LCurr.CompanyId.ToString() + "_" + LCurr.ProjectId.ToString() + "_" + screenId.ToString();
            int minutesToCache = screenAuthColCacheMinutes; // 1;
            DataTable dtAuthCol = cache[cacheKey] as DataTable;
            if (dtAuthCol == null)
            {
                dtAuthCol = (new AdminSystem()).GetAuthCol(screenId, LImpr, LCurr, LcSysConnString, LcAppPw); ;
                cache.Add(cacheKey, dtAuthCol, new System.Web.Caching.CacheDependency(new string[] { }, loginHandle == null ? new string[] { } : new string[] { loginHandle })
                    , System.Web.Caching.Cache.NoAbsoluteExpiration, new TimeSpan(0, minutesToCache, 0), System.Web.Caching.CacheItemPriority.AboveNormal, null);
            }
            return dtAuthCol;
        }
        protected DataTable _GetScreenLabel(int screenId)
        {
            var context = HttpContext.Current;
            var cache = context.Cache;
            string cacheKey = loginHandle + "_ScreenLabel_" + GetSystemId().ToString() + "_" + LUser.CultureId.ToString() + "_" + screenId.ToString();
            int minutesToCache = screenLabelCacheMinutes; // 1;
            DataTable dtLabel = cache[cacheKey] as DataTable;
            if (dtLabel == null)
            {
                dtLabel = (new AdminSystem()).GetScreenLabel(screenId, LUser.CultureId, LImpr, LCurr, LcSysConnString, LcAppPw);
                cache.Add(cacheKey, dtLabel, new System.Web.Caching.CacheDependency(new string[] { }, loginHandle == null ? new string[] { } : new string[] { loginHandle })
                    , System.Web.Caching.Cache.NoAbsoluteExpiration, new TimeSpan(0, minutesToCache, 0), System.Web.Caching.CacheItemPriority.AboveNormal, null);
            }
            return dtLabel;
        }
        protected DataTable _GetMenu(byte systemId, bool ignoreCache = false)
        {
            var context = HttpContext.Current;
            var cache = context.Cache;
            string cacheKey = loginHandle + "_Menu_" + systemId.ToString() + "_" + LCurr.CompanyId.ToString() + "_" + LCurr.ProjectId.ToString();
            int minutesToCache = menuCacheMinutes; // 1;
            Tuple<string, DataTable> dtCacheX = cache[cacheKey] as Tuple<string, DataTable>;
            if (dtCacheX == null || dtCacheX.Item1 != loginHandle || ignoreCache)
            {
                DataTable dtMenuItems = (new MenuSystem()).GetMenu(LUser.CultureId, systemId, LImpr, LcSysConnString, LcAppPw, 0, 0, 0);
                dtCacheX = new Tuple<string, DataTable>(loginHandle, dtMenuItems);
                cache.Insert(cacheKey, dtCacheX, new System.Web.Caching.CacheDependency(new string[] { }, loginHandle == null ? new string[] { } : new string[] { loginHandle })
                    , System.Web.Caching.Cache.NoAbsoluteExpiration, new TimeSpan(0, minutesToCache, 0), System.Web.Caching.CacheItemPriority.AboveNormal, null);
            }
            return dtCacheX.Item2;
        }
        protected string MakeCriteriaVal(DataRow dr, string val)
        {
            if (dr["DataTypeSysName"].ToString() == "DateTime") { return string.IsNullOrEmpty(val) ? new DateTime(1900, 1, 1).ToString() : val; }
            else if (dr["DataTypeSysName"].ToString() == "Byte") { return string.IsNullOrEmpty(val) ? (-1).ToString() : val; }
            else if (dr["DataTypeSysName"].ToString() == "Int16") { return string.IsNullOrEmpty(val) ? (-1).ToString() : val; }
            else if (dr["DataTypeSysName"].ToString() == "Int32") { return string.IsNullOrEmpty(val) ? (-1).ToString() : val; }
            else if (dr["DataTypeSysName"].ToString() == "Int64") { return string.IsNullOrEmpty(val) ? (-1).ToString() : val; }
            else if (dr["DataTypeSysName"].ToString() == "Single") { return string.IsNullOrEmpty(val) ? (-1).ToString() : val; }
            else if (dr["DataTypeSysName"].ToString() == "Double") { return string.IsNullOrEmpty(val) ? (-1).ToString() : val; }
            else if (dr["DataTypeSysName"].ToString() == "Byte[]") { return string.IsNullOrEmpty(val) ? val : val; }
            else if (dr["DataTypeSysName"].ToString() == "Object") { return string.IsNullOrEmpty(val) ? val : val; }
            else { return string.IsNullOrEmpty(val) ? Guid.NewGuid().ToString() : val; }

        }
        protected DataTable MakeColumns(DataTable dt, DataView dvCri)
        {
            DataColumnCollection columns = dt.Columns;
            foreach (DataRowView drv in dvCri)
            {
                if (drv["DataTypeSysName"].ToString() == "DateTime") { columns.Add(drv["ColumnName"].ToString(), typeof(DateTime)); }
                else if (drv["DataTypeSysName"].ToString() == "Byte") { columns.Add(drv["ColumnName"].ToString(), typeof(Byte)); }
                else if (drv["DataTypeSysName"].ToString() == "Int16") { columns.Add(drv["ColumnName"].ToString(), typeof(Int16)); }
                else if (drv["DataTypeSysName"].ToString() == "Int32") { columns.Add(drv["ColumnName"].ToString(), typeof(Int32)); }
                else if (drv["DataTypeSysName"].ToString() == "Int64") { columns.Add(drv["ColumnName"].ToString(), typeof(Int64)); }
                else if (drv["DataTypeSysName"].ToString() == "Single") { columns.Add(drv["ColumnName"].ToString(), typeof(Single)); }
                else if (drv["DataTypeSysName"].ToString() == "Double") { columns.Add(drv["ColumnName"].ToString(), typeof(Double)); }
                else if (drv["DataTypeSysName"].ToString() == "Byte[]") { columns.Add(drv["ColumnName"].ToString(), typeof(Byte[])); }
                else if (drv["DataTypeSysName"].ToString() == "Object") { columns.Add(drv["ColumnName"].ToString(), typeof(Object)); }
                else { columns.Add(drv["ColumnName"].ToString(), typeof(String)); }
            }
            return dt;
        }

        protected string ValidatedCriVal(int screenId, DataRowView drv, string val, bool refresh)
        {
            if (drv["DisplayName"].ToString() == "ListBox"
                ||
                drv["DisplayName"].ToString() == "ComboBox"
                ||
                drv["DisplayName"].ToString() == "DropDownList"
                ||
                drv["DisplayName"].ToString() == "RadioButtonList"
                )
            {
                int CriCnt = (new AdminSystem()).CountScrCri(drv["ScreenCriId"].ToString(), drv["MultiDesignDb"].ToString(), LcSysConnString, LcAppPw);
                DataTable dtScreenIn = _GetScreenIn(screenId, drv, refresh);
                var dictAllowedChoice = dtScreenIn.AsEnumerable().ToDictionary(dr => dr[drv["DdlKeyColumnName"].ToString()].ToString());
                try
                {
                    int TotalChoiceCnt = new DataView((new AdminSystem()).GetScreenIn(screenId.ToString(), "GetDdl" + drv["ColumnName"].ToString() + GetSystemId().ToString() + "C" + drv["ScreenCriId"].ToString(), CriCnt, drv["RequiredValid"].ToString(), 0, string.Empty, true, string.Empty, LImpr, LCurr, drv["MultiDesignDb"].ToString() == "N" ? LcAppConnString : LcSysConnString, LcAppPw)).Count;
                    var selectedVals = (val ?? "").Split(new char[] { ',' });
                    var matchedVals = string.Join(",",
                        selectedVals
                        .Where(v => { try { return !string.IsNullOrEmpty(v) && (dictAllowedChoice.ContainsKey(v) || v == "0" || v == "'0'"); } catch { return false; } })
                        .Select(v => drv["DisplayName"].ToString() == "ListBox" ? string.Format(v.Contains("'") ? "{0}" : "'{0}'", v) : v)
                        .ToList());
                    bool noneSelected = string.IsNullOrEmpty(matchedVals) || matchedVals == "''" || string.IsNullOrEmpty(val);
                    if (drv["DisplayName"].ToString() == "ListBox")
                    {
                        return noneSelected && CriCnt + 1 > TotalChoiceCnt ? "'-1'" : (string.IsNullOrEmpty(val) ? null : "(" + val + ")");
                    }
                    else return matchedVals;
                }
                catch (Exception ex)
                {
                    ErrorTracing(new Exception(string.Format("{0} {1}", LcAppConnString, GetSystemId()), ex));
                    throw;
                }
            }
            else if (",DateUTC,DateTimeUTC,ShortDateTimeUTC,LongDateTimeUTC,".IndexOf("," + drv["DisplayMode"].ToString() + ",") >= 0) {
                return val;
            }
            else if (drv["DisplayName"].ToString().Contains("Date")) {
                return val;
            }
            else return val;
        }
        protected bool IsGridOnlyScreen()
        {
            return GetMstTableName() == GetDtlTableName();
        }
        protected DataSet MakeScrCriteria(int screenId, DataView dvCri, List<string> lastScrCri, bool refresh, bool isSave)
        {
            DataSet ds = new DataSet();
            DataTable dtScreenCriHlp = _GetScreenCriHlp(screenId);

            ds.Tables.Add(MakeColumns(new DataTable("DtScreenIn"), dvCri));
            int ii = 0;
            DataRow dr = ds.Tables["DtScreenIn"].NewRow();
            DataRowCollection drcScreenCriHlp = dtScreenCriHlp.Rows;
            foreach (DataRowView drv in dvCri)
            {
                string val = ValidatedCriVal(screenId, drv, lastScrCri.Count > ii ? lastScrCri[ii] : null,refresh);
                
                if (drv["RequiredValid"].ToString() == "Y" && string.IsNullOrEmpty(val) && isSave)
                {
                    string columnHeader = (drcScreenCriHlp.Count > ii) ? drcScreenCriHlp[ii]["ColumnHeader"].ToString() : drv["ColumnName"].ToString();
                    string columnMsg = (drcScreenCriHlp.Count > ii) ? drcScreenCriHlp[ii]["ColumnHeader"].ToString() : drv["ColumnName"].ToString();
                    throw new Exception(columnHeader + "cannot be empty ");
                }
                else
                {
                    if (!string.IsNullOrEmpty(val) || drv["RequiredValid"].ToString() == "Y") dr[drv["ColumnName"].ToString()] = (object)MakeCriteriaVal(drv.Row, val);
                }
                ii = ii + 1;
            }
            ds.Tables["DtScreenIn"].Rows.Add(dr);
            return ds;
        }
        protected DataSet MakeScrCriteria(int screenId, DataView dvCri, SerializableDictionary<string,object> lastScrCri,bool refresh, bool isSave)
        {
            var x = dvCri.Table.AsEnumerable().Select(dr => lastScrCri.ContainsKey(dr["ColumnName"].ToString()) ? lastScrCri[dr["ColumnName"].ToString()].ToString() : null).ToList();
            return MakeScrCriteria(screenId, dvCri, x, refresh, isSave);
        }

        protected DataSet MakeScrCriteria(int screenId, DataView dvCri, DataTable dtLastScrCri, bool refresh,bool isSave)
        {
            return MakeScrCriteria(screenId, dvCri, dtLastScrCri.AsEnumerable().Skip(1).Select(dr => dr["LastCriteria"].ToString()).ToList<string>(),refresh,isSave);
        }

        protected DataView GetCriCache(int systemId, int screenId)
        {
            return new DataView(_GetScrCriteria(screenId));
        }

        protected byte[] ResizeImage(byte[] image, int maxHeight = 360)
        {

            byte[] dc;

            System.Drawing.Image oBMP = null;

            using (var ms = new MemoryStream(image))
            {
                oBMP = System.Drawing.Image.FromStream(ms);
                ms.Close();
            }

            UInt16 orientCode = 1;

            try
            {
                using (var ms2 = new MemoryStream(image))
                {
                    var r = new ExifLib.ExifReader(ms2);
                    r.GetTagValue(ExifLib.ExifTags.Orientation, out orientCode);
                }
            }
            catch { }

            int nHeight = maxHeight < oBMP.Height ? maxHeight : oBMP.Height; // This is 36x10 line:7700 GenScreen
            int nWidth = int.Parse((Math.Round(decimal.Parse(oBMP.Width.ToString()) * (nHeight / decimal.Parse(oBMP.Height.ToString())))).ToString());

            var nBMP = new System.Drawing.Bitmap(oBMP, nWidth, nHeight);
            using (System.IO.MemoryStream sm = new System.IO.MemoryStream())
            {
                // 1 = do nothing
                if (orientCode == 3)
                {
                    // rotate 180
                    nBMP.RotateFlip(System.Drawing.RotateFlipType.Rotate180FlipNone);
                }
                else if (orientCode == 6)
                {
                    //rotate 90
                    nBMP.RotateFlip(System.Drawing.RotateFlipType.Rotate90FlipNone);
                }
                else if (orientCode == 8)
                {
                    // same as -90
                    nBMP.RotateFlip(System.Drawing.RotateFlipType.Rotate270FlipNone);
                }
                nBMP.Save(sm, System.Drawing.Imaging.ImageFormat.Jpeg);
                sm.Position = 0;
                dc = new byte[sm.Length + 1];
                sm.Read(dc, 0, dc.Length); sm.Close();
            }
            oBMP.Dispose(); nBMP.Dispose();

            return dc;
        }

        protected List<_ReactFileUploadObj> DestructureFileUploadObject(string docJson)
        {
            System.Web.Script.Serialization.JavaScriptSerializer jss = new System.Web.Script.Serialization.JavaScriptSerializer();
            jss.MaxJsonLength = Int32.MaxValue;
            try {
                FileUploadObj fileObj = jss.Deserialize<FileUploadObj>(docJson);
                return new List<_ReactFileUploadObj>() { new _ReactFileUploadObj() { base64 = fileObj.base64, fileName = fileObj.fileName, lastModified = fileObj.lastModified, mimeType = fileObj.mimeType } };
            } 
            catch {
                List<_ReactFileUploadObj> reactFileArray = jss.Deserialize<List<_ReactFileUploadObj>>(docJson);
                return reactFileArray;
            }

        }
        protected List<_ReactFileUploadObj> AddDoc(string docJson, string docId, string tableName, string keyColumnName, string columnName, bool resizeToIcon = false)
        {
            byte[] storedContent = null;
            bool dummyImage = false;
            Func<List<_ReactFileUploadObj>, List<_ReactFileUploadObj>> resizeImages = (l) =>
            {
                if (!resizeToIcon) return l;
                List<_ReactFileUploadObj> x = new List<_ReactFileUploadObj>();
                foreach (_ReactFileUploadObj fileObj in l)
                {
                    try
                    {
                        dummyImage = fileObj.base64 == "iVBORw0KGgoAAAANSUhEUgAAAhwAAAABCAQAAAA/IL+bAAAAFElEQVR42mN89p9hFIyCUTAKSAIABgMB58aXfLgAAAAASUVORK5CYII=";
                        byte[] content = Convert.FromBase64String(fileObj.base64);
                        if (fileObj.base64.Length > 0 && (fileObj.mimeType ?? "application/octet-stream").StartsWith("image/"))
                        {
                            try
                            {
                                content = ResizeImage(Convert.FromBase64String(fileObj.base64));
                            }
                            catch
                            {
                            }
                        }
                        x.Add(new _ReactFileUploadObj() { base64 = Convert.ToBase64String(content), fileName = fileObj.fileName, lastModified = fileObj.lastModified, mimeType = fileObj.mimeType });
                    }
                    catch
                    {
                    }
                }
                return x;
            }
            ;
            try
            {
                System.Web.Script.Serialization.JavaScriptSerializer jss = new System.Web.Script.Serialization.JavaScriptSerializer();
                jss.MaxJsonLength = Int32.MaxValue;
                List<_ReactFileUploadObj> fileArray = DestructureFileUploadObject(docJson);
                //FileUploadObj fileObj = jss.Deserialize<FileUploadObj>(docJson);
                _ReactFileUploadObj fileObj = fileArray.Count > 0 ? fileArray[0] : new _ReactFileUploadObj();
                bool backwardCompatible = false;
                if (fileArray.Count == 0 || 
                    (fileArray.Count == 1 
                    && fileArray[0].base64 == "iVBORw0KGgoAAAANSUhEUgAAAhwAAAABCAQAAAA/IL+bAAAAFElEQVR42mN89p9hFIyCUTAKSAIABgMB58aXfLgAAAAASUVORK5CYII=")
                    )
                {
                    // empty list means DELETE 
                    new AdminSystem().UpdDbImg(docId, tableName, keyColumnName, columnName, null, LcAppConnString, LcAppPw);
                }
                else if (fileArray.Where(f => string.IsNullOrEmpty(f.base64)).Count() > 0
                    // only if there is internal inconsistency, ignore with single file case as if nothing happens
                    //    && fileArray.Count > 1
                    )
                {
                    throw new Exception("invalid file upload format, empty base64 conent");
                }
                else if (fileArray.Count > (backwardCompatible ? 1 : 0)
                        && fileArray.Where(f => !string.IsNullOrEmpty(f.base64)).Count() > 0
                    )
                {
                    var resizedFiles = resizeImages(fileArray);
                    byte[] fileStreamHeader = EncodeFileStreamHeader(resizeToIcon ? resizedFiles : fileArray);
                    byte[] content = System.Text.UTF8Encoding.UTF8.GetBytes(resizeToIcon ? jss.Serialize(resizeImages) : docJson);
                    storedContent = new byte[content.Length + fileStreamHeader.Length];
                    Array.Copy(fileStreamHeader, storedContent, fileStreamHeader.Length);
                    Array.Copy(content, 0, storedContent, fileStreamHeader.Length, content.Length);
                    new AdminSystem().UpdDbImg(docId, tableName, keyColumnName, columnName, storedContent, LcAppConnString, LcAppPw);
                    return resizedFiles;
                }
                else if (!string.IsNullOrEmpty(fileObj.base64))
                {
                    byte[] content = Convert.FromBase64String(fileObj.base64);
                    dummyImage = fileObj.base64 == "iVBORw0KGgoAAAANSUhEUgAAAhwAAAABCAQAAAA/IL+bAAAAFElEQVR42mN89p9hFIyCUTAKSAIABgMB58aXfLgAAAAASUVORK5CYII=";
                    if (resizeToIcon && fileObj.base64.Length > 0 && (fileObj.mimeType ?? "application/octet-stream").StartsWith("image/"))
                    {
                        try
                        {
                            content = ResizeImage(Convert.FromBase64String(fileObj.base64));
                        }
                        catch
                        {
                        }
                    }
                    byte[] fileStreamHeader = EncodeFileStreamHeader(fileObj);
                    if (content.Length == 0 || dummyImage)
                    {
                        storedContent = null;
                    }
                    else if ((fileObj.mimeType ?? "application/octet-stream").StartsWith("image/") && true)
                    {
                        // backward compatability with asp.net side, only store image and not fileinfo
                        storedContent = content;
                    }
                    else
                    {
                        storedContent = new byte[content.Length + fileStreamHeader.Length];
                        Array.Copy(fileStreamHeader, storedContent, fileStreamHeader.Length);
                        Array.Copy(content, 0, storedContent, fileStreamHeader.Length, content.Length);
                    }

                    new AdminSystem().UpdDbImg(docId, tableName, keyColumnName, columnName, content.Length == 0 || dummyImage ? null : storedContent, LcAppConnString, LcAppPw);

                    return resizeToIcon
                        && content.Length > 0
                        && !dummyImage
                        ? new List<_ReactFileUploadObj>() { 
                                new _ReactFileUploadObj() { 
                                    base64 = Convert.ToBase64String(content), 
                                    fileName = fileObj.fileName, 
                                    lastModified = fileObj.lastModified, 
                                    mimeType = fileObj.mimeType 
                                }
                            }
                        : null;
                }
                else
                {
                    // no content means unchanged
                }
                return null;
            }
            catch (Exception ex) { throw new Exception("invalid attachment format " + (string.IsNullOrEmpty(docId) ? "missing master key id" : ex.Message)); }
        }

        /// <summary>
        /// Returns a site relative HTTP path from a partial path starting out with a ~.
        /// Same syntax that ASP.Net internally supports but this method can be used
        /// outside of the Page framework.
        /// 
        /// Works like Control.ResolveUrl including support for ~ syntax
        /// but returns an absolute URL.
        /// </summary>
        /// <param name="originalUrl">Any Url including those starting with ~</param>
        /// <returns>relative url</returns>
        public static string ResolveUrl(string originalUrl)
        {
            if (originalUrl == null)
                return null;

            // *** Absolute path - just return
            if (originalUrl.IndexOf("://") != -1)
                return originalUrl;

            // *** Fix up image path for ~ root app dir directory
            if (originalUrl.StartsWith("~"))
            {
                string newUrl = "";
                if (HttpContext.Current != null)
                    newUrl = HttpContext.Current.Request.ApplicationPath +
                          originalUrl.Substring(1).Replace("//", "/");
                else
                    // *** Not context: assume current directory is the base directory
                    throw new ArgumentException("Invalid URL: Relative URL not allowed.");

                // *** Just to be sure fix up any double slashes
                return newUrl;
            }

            return originalUrl;
        }

        public static string ResolveServerUrl(string serverUrl, bool forceHttps = false)
        {
            // *** Is it already an absolute Url?
            if (serverUrl.IndexOf("://") > -1)
                return serverUrl;

            // *** Start by fixing up the Url an Application relative Url
            string newUrl = ResolveUrl(serverUrl);

            Uri originalUri = HttpContext.Current.Request.Url;
            newUrl = (forceHttps ? "https" : originalUri.Scheme) +
                     "://" + originalUri.Authority + newUrl;

            return newUrl;
        } 

        protected bool IsProxy()
        {
            var Request = HttpContext.Current.Request;

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

        protected string ResolveUrlCustom(string relativeUrl, bool isInternal = false)
        {
            var Request = HttpContext.Current.Request;

            string url = ResolveUrl(relativeUrl);
            string extBasePath = Config.ExtBasePath;
            string extDomain = Config.ExtDomain;
            string extBaseUrl = Config.ExtBaseUrl;
            string xForwardedFor = Request.Headers["X-Forwarded-For"];
            string xForwardedHost = HttpContext.Current.Request.Headers["X-Forwarded-Host"];
            string xForwardedProto = Request.Headers["X-Forwarded-Proto"];

            string xOriginalUrl = Request.Headers["X-Orginal-URL"];
            string host = Request.Url.Host;
            string appPath = Request.ApplicationPath;
            if (IsProxy()
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
                return Utils.transformProxyUrl(url, requestHeader);
            }
            else
            {
                return url;
            }
        }

        // Overload to handle customized SMTP configuration.
        private Int32 SendEmail(string subject, string body, string to, string from, string replyTo, string fromTitle, bool isHtml, string smtp, string bcc=null)
        {
            return SendEmail(subject, body, to, from, replyTo, fromTitle, isHtml, new List<System.Net.Mail.Attachment>(), smtp, bcc);
        }

        // "to" may contain email addresses separated by ";".
        protected Int32 SendEmail(string subject, string body, string to, string from, string replyTo, string fromTitle, bool isHtml, string bcc=null)
        {
            return SendEmail(subject, body, to, from, replyTo, fromTitle, isHtml, new List<KeyValuePair<string, byte[]>> { }, bcc);
        }

        // Overload to handle attachments and being called by the above.
        protected Int32 SendEmail(string subject, string body, string to, string from, string replyTo, string fromTitle, bool isHtml, List<KeyValuePair<string, byte[]>> att, string bcc = null)
        {
            List<System.Net.Mail.Attachment> mailAtts = new List<System.Net.Mail.Attachment>();
            foreach (var f in att)
            {
                var ms = new MemoryStream(f.Value);
                mailAtts.Add(new System.Net.Mail.Attachment(ms, f.Key));
            }
            return SendEmail(subject, body, to, from, replyTo, fromTitle, isHtml, mailAtts, string.Empty, bcc);
        }

        // Overload to handle attachments and being called by the above and should not be called publicly.
        // Return number of emails sent today; users should not exceed 10,000 a day in order to avoid smtp IP labelled as spam email.
        protected Int32 SendEmail(string subject, string body, string to, string from, string replyTo, string fromTitle, bool isHtml, List<System.Net.Mail.Attachment> att, string smtp, string bcc=null)
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
            string[] bccRecipients = bcc != null ? bcc.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries) : new string[]{};
            foreach (var t in receipients)
            {
                mm.To.Add(new System.Net.Mail.MailAddress(t.Trim()));
            }
            foreach (var t in bccRecipients)
            {
                mm.Bcc.Add(new System.Net.Mail.MailAddress(t.Trim()));
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
        protected string EncodeZipDownloadRequest(ZipDownloadRequest request)
        {
            return EncodeRequest<ZipDownloadRequest>(request);
        }
        protected ZipDownloadRequest DecodeZipDownloadRequest(string encodedRequest)
        {
            return DecodeRequest<ZipDownloadRequest>(encodedRequest);
        }
        protected virtual string MakeZipAllParam(string mstId, string jsonColumnParam, string fileName)
        {
            System.Web.Script.Serialization.JavaScriptSerializer jss = new System.Web.Script.Serialization.JavaScriptSerializer();
            List<Dictionary<string, string>> selectedDocList = string.IsNullOrEmpty(jsonColumnParam)
                                                                ? null
                                                                : jss.Deserialize<List<Dictionary<string, string>>>(jsonColumnParam);
            Func<string, Dictionary<string, string>> selectedColumn =
                columnName => selectedDocList == null
                    ? null
                    : selectedDocList.Where(d => d.ContainsKey("ColumnName") && !string.IsNullOrEmpty(d["ColumnName"]) && d["ColumnName"] == columnName).FirstOrDefault();

            byte dbId = GetDbId();
            byte sysId = GetSystemId();
            int screenId = GetScreenId();

            List<RO.Web.ZipMultiDocRequest> md = new List<RO.Web.ZipMultiDocRequest>
            {
            };

            DataTable dtAut = _GetAuthCol(screenId);
            DataTable dtLabel = _GetScreenLabel(screenId);
            int ii = 0;
            foreach (DataRow drLabel in dtLabel.Rows)
            {
                DataRow drAuth = dtAut.Rows[ii];
                bool colVisible = drAuth["ColVisible"].ToString() == "Y";
                if (!string.IsNullOrEmpty(drLabel["TableId"].ToString())
                    && colVisible
                    && drAuth["MasterTable"].ToString() == "Y"
                    && drLabel["DisplayMode"].ToString() == "Document"
                    )
                {
                    string colName = drLabel["ColumnName"].ToString() + drLabel["TableId"].ToString();
                    var col = selectedColumn(colName);
                    string tableColumnName = drLabel["ColumnName"].ToString();
                    string dirName = col != null && col.ContainsKey("DirName") && !string.IsNullOrEmpty(col["DirName"]) ? col["DirName"] : tableColumnName;
                    List<string> docIdLs = col != null && col.ContainsKey("IdLs") && col["IdLs"] != null
                                                ? col["IdLs"].Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList()
                                                : null
                                                ;
                    if (selectedDocList == null
                        || col != null)
                    {
                        md.Add(MakeZipMultiDocRequest(sysId, screenId, mstId
                            , "GetDdl" + tableColumnName + GetSystemId() + "S" + drLabel["ScreenObjId"].ToString(), drLabel["ColumnName"].ToString(), dirName, docIdLs));
                    }
                }
                ii = ii + 1;
            }

            RO.Web.ZipDownloadRequest r = new RO.Web.ZipDownloadRequest()
            {
                zN = fileName,
                e = DateTime.UtcNow.AddMinutes(60).ToFileTimeUtc(),
                md = CompressZipMultiDocRequest(md)
            };
            string encodedRequest = EncodeZipDownloadRequest(r);
            return encodedRequest;
        }
        protected virtual Tuple<string, string, byte[]> ZipAllDoc(string encodedZipAllRequest)
        {
            RO.Web.ZipDownloadRequest x = DecodeZipDownloadRequest(encodedZipAllRequest);

            byte[] zipResult = GetMultiDoc(new RO.Web.ZipDownloadRequest() { zN = x.zN, md = ExpandZipMultiDocRequest(x.md), ed = x.ed });
            return new Tuple<string, string, byte[]>(x.zN, "application/zip", zipResult);
        }
        protected virtual Ionic.Zip.ZipFile GetMultiDoc(string systemId, string screenId, string mstId, string spName, string tableName, string parentDirectory, Ionic.Zip.ZipFile zipObject, string rootDirectory, string docLs = null)
        {
            byte sid = byte.Parse(systemId);
            int scrId = int.Parse(screenId);
            string dbConnectionString = LcAppConnString;
            string dbPwd = LcAppPw ;
            List<string> selectedIds = docLs == null ? null : docLs.Split(new char[]{','}, StringSplitOptions.RemoveEmptyEntries).Select(s=>s.Trim()).ToList();
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
                            foreach (var c in r.cols)
                            {
                                GetMultiDoc(r.scr[0], r.scr[1], r.scr[2], c[0], c[1], c[2], resultFile, tempDirectory, c[3]);
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
                ErrorTrace(e, "error");
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
                    ErrorTrace(new Exception("problem removing temp directory for zip usage", ex), "warning");
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
        protected virtual void ReturnAsAttachment(HttpResponse Response, byte[] content, string fileName, string mimeType = "application/octet-stream", bool inline = true)
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
        protected virtual string GetUrlWithQSHashV2(string url, bool noEncrypt = false)
        {
            /* this one must match with the validation side in ModuleBase.cs ValidateQSV2 */
            int questionMarkPos = url.IndexOf('?');
            string path = questionMarkPos >= 0 ? url.Substring(0, questionMarkPos) : url;
            string qs = questionMarkPos >= 0 ? url.Substring(questionMarkPos).Substring(1) : "";

            if (string.IsNullOrEmpty(qs)) return url;
            if (!(path.ToLower().EndsWith("~/dnload.aspx") 
                || path.ToLower().EndsWith("/dnload.aspx") 
                || path.ToLower().EndsWith("~/upload.aspx") 
                || path.ToLower().EndsWith("/upload.aspx"))
                ) return url;
            List<string> qsList = qs.Split(new char[] { '&' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            if (qsList.Where(q => q.ToLower().Contains("sys=")).Count() == 0)
            {
                qsList.Add("sys=" + GetSystemId().ToString());
            }
            string qsToHash = string.Join("&",
                        qsList.Where(s=>!s.StartsWith("inline") && !s.StartsWith("ico")).OrderBy(v => v.ToLower()).ToArray()).ToLower().Trim();

            //RandomNumberGenerator rng = new RNGCryptoServiceProvider();
            //byte[] tokenData = new byte[8];
            //rng.GetBytes(tokenData);
            byte[] tokenData = Guid.NewGuid().ToByteArray();
            Dictionary<string, string> _h = new Dictionary<string, string>();
            _h["_s"] = Convert.ToBase64String(tokenData);
            System.Security.Cryptography.HMACMD5 hmac = new System.Security.Cryptography.HMACMD5(tokenData);
            byte[] hash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(Convert.ToBase64String(tokenData) + qsToHash.ToString()));
            string hashString = Convert.ToBase64String(hash);
            _h["_h"] = hashString;
            _h["id"] = LUser.UsrId.ToString();
            _h["e"] = DateTime.UtcNow.AddMinutes(60).ToFileTimeUtc().ToString();
            string qsHash = EncodeRequest<Dictionary<string, string>>(_h, noEncrypt);
            //string qsHash = hashString;
            /* url with hash to prevent tampering of manual construction, only for dnload.aspx */
            return path + "?" + string.Join("&", qsList.ToArray()) + "&_h=" + qsHash;
        }

        #endregion

        protected DataTable GetLis(string getLisMethod, int systemId, int screenId, string searchStr, List<string> criteria, string filterId, string conn, string isSys, int topN, bool addRow = true)
        {
            // this is a system wide format and must be kept in sync with robot if there is any change of it
            DataView dvCri = GetCriCache(systemId, screenId);
            UsrCurr uc = LCurr;
            UsrImpr ui = LImpr;
            LoginUsr usr = LUser;
            int rowExpected = dvCri.Count;
            var effectiveFilterId = GetEffectiveScreenFilterId(filterId, true);
            DataTable dtLastScrCriteria = _GetLastScrCriteria(screenId, rowExpected);
            DataSet ds = criteria == null || criteria.Count == 0 ? MakeScrCriteria(screenId, dvCri, dtLastScrCriteria,true,false) : MakeScrCriteria(screenId, dvCri, criteria,true,false);
            DataTable dt = (new AdminSystem()).GetLis(screenId, getLisMethod, addRow, "Y", topN, isSys != "N" ? (string)null : LcAppConnString, isSys != "N" ? null : LcAppPw,
                string.IsNullOrEmpty(filterId) ? 0 : effectiveFilterId, searchStr.StartsWith("**") ? searchStr.Substring(2) : "", searchStr.StartsWith("**") ? "" : searchStr,
                dvCri, ui, uc, ds);

            return dt;
        }

        protected DataTable GetDdl(string getLisMethod, bool bAddNew, int systemId, int screenId, string searchStr, string conn, string isSys, string sp, string requiredValid, int topN)
        {
            UsrCurr uc = LCurr;
            UsrImpr ui = LImpr;
            LoginUsr usr = LUser;
            DataTable dt = null;
            Regex cleanup = new Regex("^undefined|null$");
            if (!string.IsNullOrEmpty(sp))
            {
                var regex = new System.Text.RegularExpressions.Regex("C[0-9]+$");
                var scrCriId = sp.Replace(regex.Replace(sp, ""), "").Replace("C", "");
                int CriCnt = (new AdminSystem()).CountScrCri(scrCriId, string.IsNullOrEmpty(conn) ? "N" : "Y", LcSysConnString, LcAppPw);
                _MkScreenIn(screenId, scrCriId, sp, isSys == "Y" ? "Y" : "N", true);
                dt = (new AdminSystem()).GetScreenIn(screenId.ToString(), sp, CriCnt, requiredValid, topN,
                searchStr.StartsWith("**") ? "" : searchStr, !searchStr.StartsWith("**"), searchStr.StartsWith("**") ? cleanup.Replace(searchStr.Substring(2),"") : "", ui, uc,
                isSys != "N" ? (string)null : LcAppConnString,
                isSys != "N" ? null : LcAppPw);
            }
            else
            {
                dt = (new AdminSystem()).GetDdl(screenId, getLisMethod, bAddNew, !searchStr.StartsWith("**"), topN, searchStr.StartsWith("**") ? cleanup.Replace(searchStr.Substring(2),"") : "",
                    isSys != "N" ? (string)null : LcAppConnString,
                    isSys != "N" ? null : LcAppPw, searchStr.StartsWith("**") ? "" : searchStr, ui, uc);
            }
            return dt;
        }

        protected void ValidateAction(int screenId, string action)
        {
            DataTable dtAuthRow = _GetAuthRow(screenId);
            if ( // screen based checking, i.e. record level
                dtAuthRow.Rows.Count == 0
                || (dtAuthRow.Rows[0]["ViewOnly"].ToString() == "Y" && (action == "S" || action == "A" || action == "U" || action == "D"))
                || (dtAuthRow.Rows[0]["AllowAdd"].ToString() == "N" && dtAuthRow.Rows[0]["AllowUpd"].ToString() == "N" && action == "S")
                || (dtAuthRow.Rows[0]["AllowAdd"].ToString() == "N" && action == "A")
                || (dtAuthRow.Rows[0]["AllowUpd"].ToString() == "N" && action == "U")
                || (dtAuthRow.Rows[0]["AllowDel"].ToString() == "N" && action == "D")
               )
            {
                throw new UnauthorizedAccessException("access denied");
            }
        }
        protected void ValidatedMstId(string getListMethod, byte csy, int screenId, string query, List<string> criteria, bool isAdd = false)
        {
            DataTable dtSuggest = GetLis(getListMethod, csy, screenId, query, criteria, "0", "", "N", 1);
            if (
                (dtSuggest.Rows.Count == 0 
                 || (dtSuggest.Rows.Count == 1
                        && (query ?? "").StartsWith("**") && (query ?? "") != "**-1" && (query ?? "") != "**" 
                        && string.IsNullOrEmpty(dtSuggest.Rows[0][GetMstKeyColumnName()].ToString()))
                )
                && !isAdd)
            {
                throw new UnauthorizedAccessException("access denied " + query);
            }
            else if (isAdd)
            {
                DataTable dtAuthRow = _GetAuthRow(screenId);
                if (dtAuthRow.Rows[0]["AllowAdd"].ToString() == "N") 
                {
                    throw new UnauthorizedAccessException("access denied on add");
                }
            }
        }
        protected string ValidatedColAuth(string screenColumnName, bool isMaster, string DisplayMode, string DisplayName, bool isUpdate)
        {
            DataTable dtAut = _GetAuthCol(GetScreenId());
            DataTable dtLabel = _GetScreenLabel(GetScreenId());
            int ii = 0;
            foreach (DataRow drLabel in dtLabel.Rows)
            {
                DataRow drAuth = dtAut.Rows[ii];
                if (
                    (
                    (drLabel["ColumnName"].ToString() + drLabel["TableId"].ToString()) == screenColumnName
                    &&
                    (string.IsNullOrEmpty(DisplayMode) && string.IsNullOrEmpty(DisplayName))
                    || drLabel["DisplayName"].ToString() == DisplayName
                    || drLabel["DisplayMode"].ToString() == DisplayMode
                    )
                    && !string.IsNullOrEmpty(drLabel["TableId"].ToString())
                    && drAuth["MasterTable"].ToString() == (isMaster ? "Y" : "N")
                    && drAuth["ColVisible"].ToString() == "Y"
                    )
                {
                    string tableColumnName = drLabel["ColumnName"].ToString();
                    return tableColumnName;
                }
                ii = ii + 1;
            }
            return "";
        }

        protected List<string> MatchScreenCriteria(DataView dvCri, string jsonCri)
        {
            List<string> scrCri = new List<string>();
            if (!string.IsNullOrEmpty(jsonCri))
            {
                try
                {
                    System.Web.Script.Serialization.JavaScriptSerializer jss = new System.Web.Script.Serialization.JavaScriptSerializer();
                    Dictionary<string, string> cri = jss.Deserialize<Dictionary<string, string>>(jsonCri ?? "{}");
                    foreach (DataRowView drv in dvCri)
                    {
                        string columnName = drv["ColumnName"].ToString();
                        scrCri.Add(cri.ContainsKey(columnName) ? cri[columnName] : null);
                    }
                }
                catch { }
            }
            return scrCri;
        }
        protected string ToStandardString(string columnName, DataRow dr, bool includeBLOB = false) 
        {
            var colType = dr[columnName].GetType();
            return
                colType == typeof(DateTime) ? ((DateTime)dr[columnName]).ToString(((DateTime)dr[columnName]).TimeOfDay.Ticks > 0 ? "o" : "yyyy.MM.dd") :
                    colType == typeof(byte[]) ? (dr[columnName] != null && includeBLOB ? DecodeFileStream((byte[])(dr[columnName]), true) : null) : dr[columnName].ToString();
        }

        protected AutoCompleteResponse LisSuggests(string query, string contextStr, int topN, List<string> currentCriteria = null)
        {
            /* this is intended to be used by keyid fields where the text suggested ties to specific key
             * in a table and not using returned value is supposed to be an error
             */
            System.Web.Script.Serialization.JavaScriptSerializer jss = new System.Web.Script.Serialization.JavaScriptSerializer();
            Dictionary<string, string> context = jss.Deserialize<Dictionary<string, string>>(contextStr);
            AutoCompleteResponse ret = new AutoCompleteResponse();
            byte csy = byte.Parse(context["csy"]);
            int screenId = int.Parse(context["scr"]);
            UsrCurr uc = LCurr;
            UsrImpr ui = LImpr;
            LoginUsr usr = LUser;
            List<string> suggestion = new List<string>();
            List<string> key = new List<string>();
            int total = 0;
            bool extendedContent = false;
            DataView dvCri = new DataView(_GetScrCriteria(screenId));
            DataTable dtSuggest = GetLis(context["method"], csy, screenId, query, currentCriteria ?? new List<string>(), context["filter"], context["conn"], context["isSys"], topN);
            string keyF = context["mKey"].ToString();
            string valF = context.ContainsKey("mVal") ? context["mVal"] : keyF + "Text";
            string valFR = context.ContainsKey("mValR") ? context["mValR"] : (dtSuggest.Columns.Contains(keyF + "TextR") ? keyF + "TextR" : "");
            string dtlF = context.ContainsKey("mDtl") && extendedContent ? context["mDtl"] : (dtSuggest.Columns.Contains(keyF + "Dtl") ? keyF + "Dtl" : "");
            string dtlFR = context.ContainsKey("mDtlR") && extendedContent ? context["mDtlR"] : (dtSuggest.Columns.Contains(keyF + "DtlR") ? keyF + "DtlR" : "");
            string tipF = context.ContainsKey("mTip") && extendedContent ? context["mTip"] : (dtSuggest.Columns.Contains(keyF + "Dtl") ? keyF + "Dtl" : "");
            string imgF = context.ContainsKey("mImg") && extendedContent ? context["mImg"] : (dtSuggest.Columns.Contains(keyF + "Img") ? keyF + "Img" : "");
            string iconUrlF = context.ContainsKey("mIconUrl") && extendedContent ? context["mIconUrl"] : (dtSuggest.Columns.Contains(keyF + "Url") ? keyF + "Url" : "");
            // optimization on return, requesting 100 may only return records beyond key value, this is assuming the sorting original sort sequence from backend
            string startKeyVal = context.ContainsKey("startKeyVal") ? context["startKeyVal"] : "";
            string startLabelVal = context.ContainsKey("startLabelVal") ? context["startLabelVal"].ToLowerInvariant() : "";
            bool bFullImage = context.ContainsKey("fullImage");
            bool valueIsKey = context.ContainsKey("valueIsKey") || true;
            bool hasDtlColumn = dtSuggest.Columns.Contains(keyF + "Dtl");
            bool hasValRColumn = dtSuggest.Columns.Contains(keyF + "TextR");
            bool hasDtlRColumn = dtSuggest.Columns.Contains(keyF + "DtlR");
            bool hasIconColumn = dtSuggest.Columns.Contains(keyF + "Url");
            bool hasImgColumn = dtSuggest.Columns.Contains(keyF + "Img");
            bool hasActive = dtSuggest.Columns.Contains("Active");
            total = dtSuggest.Rows.Count;
            int skipped = 0;
            List<SerializableDictionary<string, string>> results = new List<SerializableDictionary<string, string>>();
            Dictionary<string, string> Choices = new Dictionary<string, string>();
            DataTable dtAuthRow = _GetAuthRow(screenId);
            bool allowAdd = dtAuthRow.Rows.Count == 0 || dtAuthRow.Rows[0]["AllowAdd"].ToString() != "N";
            //dtSuggest.DefaultView.Sort = valF;
            //int pos = 1;
            string doublestar = System.Text.RegularExpressions.Regex.Escape(query.ToLower());
            query = System.Text.RegularExpressions.Regex.Escape(query.ToLower());
            System.Text.RegularExpressions.Regex regex = new System.Text.RegularExpressions.Regex(query.Replace("\\ ", ".*"));
            int matchCount = -1;
            bool hasMatchCount = dtSuggest.Columns.Contains("MatchCount");
            var rx = new Regex("^[^\\]]*\\]");
            bool dropEnded = string.IsNullOrEmpty(startKeyVal);

            foreach (DataRowView drv in dtSuggest.DefaultView)
            {
                if (hasMatchCount && matchCount < 0 && !string.IsNullOrEmpty(drv[keyF].ToString().Trim()))  int.TryParse(drv["MatchCount"].ToString(), out matchCount);

                string ss = drv[keyF].ToString().Trim();
                string label = drv[valF].ToString().Trim().ToLowerInvariant();
                bool startTaking = !string.IsNullOrEmpty(startLabelVal) && label.CompareTo(startLabelVal) > 0;
                //if (allowAdd || ss != string.Empty)
                if (ss != string.Empty)
                {
                    if (Choices.ContainsKey(ss))
                    {
                        total = total - 1;
                    }
                    else if (GetCriCache(csy, screenId).Count > 0 || regex.IsMatch(drv[valF].ToString().ToLower()) || query.StartsWith(doublestar) || string.IsNullOrEmpty(query))
                    {
                        Choices[drv[keyF].ToString()] = drv[valF].ToString();
                        if (dropEnded || startTaking)
                        {
                            //var image = imgF != "" && !string.IsNullOrEmpty(drv[imgF].ToString())
                            //            ? BlobImage(drv[imgF] as byte[], bFullImage) ?? new Tuple<string, byte[]>("application/base64", new byte[0])
                            //            : new Tuple<string, byte[]>("application/base64", new byte[0])
                            //            ;
                            var rec = new SerializableDictionary<string, string> { 
                                {"key",drv[keyF].ToString()}, // internal key 
                                {"label",drv[valF].ToString()}, // visible dropdown list as used in jquery's autocomplete
                                {"labelL",rx.Replace(drv[valF].ToString(),"")}, // stripped desc
                                {"value", valueIsKey ? drv[keyF].ToString() : drv[valF].ToString()}, // visible value shown in jquery's autocomplete box, react expect this to be the key not label
                                {"iconUrl",iconUrlF !="" ? drv[iconUrlF].ToString() == "" ? "" : ResolveUrlCustom(GetUrlWithQSHashV2(drv[iconUrlF].ToString()),false,true) : null}, // optional icon url
                                {"img", imgF !="" ? (drv[imgF].ToString() == "" ? "": RO.Common3.Utils.BlobPlaceHolder(drv[imgF] as byte[], true))  : null}, // optional embedded image
                                {"tooltips",tipF !="" ? drv[tipF].ToString() : ""},// optional alternative tooltips(say expanded description)
                                {"detail",dtlF !="" ? ToStandardString(dtlF,drv.Row) : null}, // optional detail info
                                {"labelR",valFR !="" ? ToStandardString(valFR,drv.Row) : null}, // optional title(right hand side for react presentation)
                                {"detailR",dtlFR !="" ? ToStandardString(dtlFR,drv.Row) : null} // optional detail info(right hand side for react presentation)
                            /* more can be added in the future for say multi-column list */
                            };
                            if (hasActive) rec["Active"] = drv["Active"].ToString();
                            results.Add(rec);
    
                        }
                        else 
                        {
                            dropEnded = (ss == startKeyVal || (label.CompareTo(startLabelVal) >= 0));
                            skipped = skipped + 1;
                        }
                    }
                    else
                    {
                        total = total - 1;
                        matchCount = matchCount - 1;
                    }
                    if (Choices.Count >= (topN > 0 ? topN : 15)) break;
                }
                else
                {
                    total = total - 1;
                }
            }

            /* returning data */
            ret.query = query;
            ret.data = results;
            ret.total = total;
            ret.skipped = skipped;
            ret.topN = topN;
            ret.matchCount = matchCount < 0 ? 0 : matchCount;

            return ret;
        }

        protected string getNonEmptyStr(string str)
        {
            return (!string.IsNullOrEmpty(str)) ? str : " ";
        }

        protected AutoCompleteResponse ddlSuggests(string inQuery, Dictionary<string, string> context, int topN)
        {
            /* this is intended to be used by keyid fields where the text suggested ties to specific key
             * in a table and not using returned value is supposed to be an error
             */
            AutoCompleteResponse ret = new AutoCompleteResponse();
            byte csy = byte.Parse(context["csy"]);
            int screenId = int.Parse(context["scr"]);
            UsrCurr uc = LCurr;
            UsrImpr ui = LImpr;
            LoginUsr usr = LUser;
            List<string> suggestion = new List<string>();
            List<string> key = new List<string>();
            int total = 0;
            bool extendedContent = false;
            DataTable dtSuggest = GetDdl(context["method"], context["addnew"] == "Y", csy, screenId, inQuery, context["conn"], context["isSys"], context.ContainsKey("sp") ? context["sp"] : "", context.ContainsKey("requiredValid") ? context["requiredValid"] : "", (context.ContainsKey("refCol") && !string.IsNullOrEmpty(context["refCol"])) || (context.ContainsKey("pMKeyCol") && !string.IsNullOrEmpty(context["pMKeyCol"])) ? 0 : topN);
            string keyF = context["mKey"].ToString();
            string valF = context.ContainsKey("mVal") ? context["mVal"] : keyF + "Text";
            string tipF = context.ContainsKey("mTip") ? context["mTip"] : "";
            string imgF = context.ContainsKey("mImg") && extendedContent ? context["mImg"] : "";
            bool valueIsKey = context.ContainsKey("valueIsKey") || true;
            bool hasActive = dtSuggest.Columns.Contains("Active");

            total = dtSuggest.Rows.Count;
            List<SerializableDictionary<string, string>> results = new List<SerializableDictionary<string, string>>();
            Dictionary<string, string> Choices = new Dictionary<string, string>();
            DataTable dtAuthRow = _GetAuthRow(screenId);
            string[] DesiredKeys = inQuery.StartsWith("**") ? inQuery.Substring(2).Replace("(", "").Replace(")", "").Replace("undefined", "").Replace("null", "").Split(new char[] { ',' }) : new string[0];
            string query = System.Text.RegularExpressions.Regex.Escape(inQuery.ToLower());
            string doublestar = System.Text.RegularExpressions.Regex.Escape("**");
            System.Text.RegularExpressions.Regex regex = new System.Text.RegularExpressions.Regex(query.Replace("\\ ", ".*"));
            //dtSuggest.DefaultView.Sort = valF;
            string filter = "";
            string[] needQuoteType = { "Char", "Date", "Time", "String" };
            if (context.ContainsKey("refCol") && !string.IsNullOrEmpty(context["refCol"]))
            {
                string[] x = context["refCol"].Split(new char[] { '_' });
                bool isList = context.ContainsKey("refColValIsList") && context["refColValIsList"] == "Y";
                string[] filterColumnIsIntType = { "Int", "SmallInt", "TinyInt", "BigInt" };
                bool filterColumnIsList = context.ContainsKey("refColDataType") && "TinyInt,SmallInt,Int,BigInt,Char,NChar".IndexOf(context["refColDataType"]) >= 0 && dtSuggest.Columns[x[x.Length - 1]].DataType.ToString().Contains("String");
                try
                {
                    if (filterColumnIsList)
                    {
                        if ("Char,NChar".IndexOf(context["refColDataType"]) >= 0)
                            filter = string.Format(" (',' + SUBSTRING(ISNULL({0},'()'),2,LEN(ISNULL({0},'()'))-2) + ',' LIKE '%,''{1}'',%' OR {0} IS NULL) ", x[x.Length - 1], context["refColVal"].Replace("'", "''"));
                        else
                            filter = string.Format(" (',' + SUBSTRING(ISNULL({0},'()'),2,LEN(ISNULL({0},'()'))-2) + ',' LIKE '%,{1},%' OR {0} IS NULL) ", x[x.Length - 1], context["refColVal"].Replace("'", "''"));
                    }
                    else
                    {
                        bool needQuote = needQuoteType.Any(dtSuggest.Columns[x[x.Length - 1]].DataType.ToString().Contains);
                        if (needQuote)
                        {
                            if (isList)
                                filter = string.Format(" ({0} IN ({1}) OR {0} IS NULL) ", x[x.Length - 1], "'" + context["refColVal"].Replace("'", "''").Replace(",", "','") + "'");
                            else
                            {
                                filter = string.Format(" ({0} = '{1}' OR {0} IS NULL) ", x[x.Length - 1], context["refColVal"].Replace("'", "''"));
                            }

                        }
                        else
                        {
                            if (isList)
                                filter = string.Format(" ({0} IN ({1}) OR {0} IS NULL) ", x[x.Length - 1], context["refColVal"]);
                            else
                            {
                                filter = string.Format(" ({0} = {1} OR {0} IS NULL) ", x[x.Length - 1], context["refColVal"]);
                            }

                        }
                    }

                }
                catch { }
            }
            if (context.ContainsKey("pMKeyCol") && !string.IsNullOrEmpty(context["pMKeyCol"]) && dtSuggest.Columns.Contains(context["pMKeyCol"]))
            {
                string[] x = context["pMKeyCol"].Split(new char[] { '_' });
                try
                {
                    bool needQuote = needQuoteType.Any(dtSuggest.Columns[x[x.Length - 1]].DataType.ToString().Contains);
                    if (needQuote)
                        filter = filter + (!string.IsNullOrEmpty(filter) ? " AND " : string.Empty) + string.Format(" ({0} = '{1}' OR {0} IS NULL) ", x[x.Length - 1], context["pMKeyColVal"].Replace("'", "''"));
                    else
                        filter = filter + (!string.IsNullOrEmpty(filter) ? " AND " : string.Empty) + string.Format(" ({0} = {1} OR {0} IS NULL) ", x[x.Length - 1], context["pMKeyColVal"]);
                }
                catch { }
            }
            try
            {
                if (!string.IsNullOrEmpty(filter))
                {
                    dtSuggest.DefaultView.RowFilter = filter;
                    total = dtSuggest.DefaultView.Count;
                }
            }
            catch { }
            foreach (DataRowView drv in dtSuggest.DefaultView)
            {
                string ss = drv[keyF].ToString().Trim();
                if (ss == string.Empty || ss != string.Empty) // include empty line for quick empty out selection
                {
                    if (Choices.ContainsKey(ss) || (query.StartsWith(doublestar) && !DesiredKeys.Contains(ss) && !DesiredKeys.Contains("'" + ss + "'")))
                    {
                        total = total - 1;
                    }
                    else if (regex.IsMatch(drv[valF].ToString().ToLower()) || query.StartsWith(doublestar) || string.IsNullOrEmpty(query))
                    {
                        Choices[drv[keyF].ToString()] = drv[valF].ToString();
                        var rec = new SerializableDictionary<string, string> { 
                            {"key", getNonEmptyStr(drv[keyF].ToString())}, // internal key 
                            {"label", getNonEmptyStr(drv[valF].ToString())}, // visible dropdown list as used in jquery's autocomplete
                            {"value", getNonEmptyStr(valueIsKey ? drv[keyF].ToString() : drv[valF].ToString())}, // visible value shown in jquery's autocomplete box, react expect it to be key not label
                            {"img", imgF !="" ? RO.Common3.Utils.BlobPlaceHolder(drv[imgF] as byte[],true) : null}, // optional image
                            {"tooltips",tipF !="" ? drv[tipF].ToString() : ""} // optional alternative tooltips(say expanded description)
                            /* more can be added in the future for say multi-column list */
                            };
                        if (hasActive) rec["Active"] = drv["Active"].ToString();
                        results.Add(rec);
                    }
                    else
                    {
                        total = total - 1;
                    }
                    /* use 25 to match default front end of react autocomplete to show the ... */
                    //if (Choices.Count >= (topN > 0 ? topN : 15)) break;
                    if (Choices.Count >= (topN > 0 ? topN : 25)) break;
                }
                else
                {
                    total = total - 1;
                }
            }

            /* returning data */
            ret.query = query;
            ret.data = results;
            ret.total = total;
            ret.topN = topN;

            return ret;
        }

        protected Dictionary<string, DataRow> GetScreenMenu(byte systemId, int screenId)
        {
            var context = HttpContext.Current;
            var cache = context.Cache;
            string cacheKey = loginHandle + "_ScreenMenu_" + systemId.ToString() + "_" + LCurr.CompanyId.ToString() + "_" + LCurr.ProjectId.ToString();
            int minutesToCache = screenMenuCacheMinutes; // 1;
            Tuple<string, Dictionary<string, DataRow>> mCacheX = cache[cacheKey] as Tuple<string, Dictionary<string, DataRow>>;

            if (mCacheX == null || mCacheX.Item1 != loginHandle || !mCacheX.Item2.ContainsKey(screenId.ToString()))
            {
                Dictionary<string, DataRow> m = mCacheX == null || mCacheX.Item1 != loginHandle ? new Dictionary<string, DataRow>() : mCacheX.Item2;
                DataTable dtMenu = (new MenuSystem()).GetMenu(LUser.CultureId, systemId, LImpr, LcSysConnString, LcAppPw, screenId, null, null);
                if (dtMenu.Rows.Count > 0)
                {
                    m[screenId.ToString()] = dtMenu.Rows[0];
                }
                if (mCacheX == null || mCacheX.Item1 != loginHandle)
                {
                    mCacheX = new Tuple<string, Dictionary<string, DataRow>>(loginHandle, m);
                    cache.Insert(cacheKey, mCacheX, new System.Web.Caching.CacheDependency(new string[] { }, loginHandle == null ? new string[] { } : new string[] { loginHandle })
                        , System.Web.Caching.Cache.NoAbsoluteExpiration, new TimeSpan(0, minutesToCache, 0), System.Web.Caching.CacheItemPriority.AboveNormal, null);
                }
            }
            return mCacheX.Item2;
        }
        protected byte[] EncodeFileStreamHeader(_ReactFileUploadObj fileObj)
        {
            /* store as 256 byte UTF8 json header + actual binary file content 
              * if header info > 256 bytes use compact header(256 bytes) + actual header + actual binary file content
              */
            System.Web.Script.Serialization.JavaScriptSerializer jss = new System.Web.Script.Serialization.JavaScriptSerializer();
            jss.MaxJsonLength = Int32.MaxValue;
            string contentHeader = jss.Serialize(new FileInStreamObj() { fileName = fileObj.fileName, lastModified = fileObj.lastModified, mimeType = fileObj.mimeType, ver = "0100", height=fileObj.height, width=fileObj.width, size=fileObj.size, extensionSize = 0 });
            byte[] streamHeader = Enumerable.Repeat((byte)0x20, 256).ToArray();
            int headerLength = System.Text.UTF8Encoding.UTF8.GetBytes(contentHeader).Length;
            string compactHeader = jss.Serialize(new FileInStreamObj() { fileName = "", lastModified = fileObj.lastModified, mimeType = fileObj.mimeType, ver = "0100", height=fileObj.height, width=fileObj.width, size=fileObj.size, extensionSize = headerLength });
            int compactHeaderLength = System.Text.UTF8Encoding.UTF8.GetBytes(compactHeader).Length;
            if (headerLength <= 256)
                Array.Copy(System.Text.UTF8Encoding.UTF8.GetBytes(contentHeader), streamHeader, headerLength);
            else
            {
                Array.Resize(ref streamHeader, 256 + headerLength);
                Array.Copy(System.Text.UTF8Encoding.UTF8.GetBytes(compactHeader), streamHeader, compactHeaderLength);
                Array.Copy(System.Text.UTF8Encoding.UTF8.GetBytes(compactHeader), 0, streamHeader, 255, headerLength);
            }
            return streamHeader;
        }
        protected byte[] EncodeFileStreamHeader(List<_ReactFileUploadObj> fileObjList)
        {
            /* store as 256 byte UTF8 json header + actual JSON stream(whatever it is ) 
              */
            System.Web.Script.Serialization.JavaScriptSerializer jss = new System.Web.Script.Serialization.JavaScriptSerializer();
            jss.MaxJsonLength = Int32.MaxValue;
            byte[] streamHeader = Enumerable.Repeat((byte)0x20, 256).ToArray();
            int headerLength = 0;
            string compactHeader = jss.Serialize(new FileInStreamObj() { fileName = "", contentIsJSON = true, extensionSize = 0 });
            int compactHeaderLength = System.Text.UTF8Encoding.UTF8.GetBytes(compactHeader).Length;
            Array.Resize(ref streamHeader, 256 + headerLength);
            Array.Copy(System.Text.UTF8Encoding.UTF8.GetBytes(compactHeader), streamHeader, compactHeaderLength);
            Array.Copy(System.Text.UTF8Encoding.UTF8.GetBytes(compactHeader), 0, streamHeader, 255, headerLength);
            return streamHeader;
        }
        protected string DecodeFileStream(byte[] content, bool checkJSONSize, bool headerOnly = false)
        {
            byte[] header = null;

            if (content != null && content.Length >= 256)
            {
                header = new byte[256];
                Array.Copy(content, header, 256);
            }
            string retVal = null;
            int MaxJsonLength = -1; // 
            if (header != null)
            {
                try
                {
                    System.Web.Script.Serialization.JavaScriptSerializer jss = new System.Web.Script.Serialization.JavaScriptSerializer();
                    jss.MaxJsonLength = Int32.MaxValue;
                    string headerString = System.Text.UTF8Encoding.UTF8.GetString(header);
                    FileInStreamObj fileInfo = jss.Deserialize<FileInStreamObj>(headerString.Substring(0, headerString.IndexOf('}') + 1));
                    int extensionSize = fileInfo.extensionSize;
                    if (extensionSize > 0)
                    {
                        header = new byte[extensionSize];
                        Array.Copy(content, 256, header, 0, extensionSize);
                        headerString = System.Text.UTF8Encoding.UTF8.GetString(header);
                        fileInfo = jss.Deserialize<FileInStreamObj>(headerString.Substring(0, headerString.IndexOf('}') + 1));
                    }
                    if (headerOnly
                        &&
                        (
                        content.Length <= 256
                        ||
                        !(fileInfo.mimeType ?? "").Contains("image")
                        ||
                        !string.IsNullOrWhiteSpace(fileInfo.previewUrl)
                        )
                        )
                    {
                        retVal = jss.Serialize(new FileUploadObj()
                        {
                            base64 = null
                            , previewUrl = fileInfo.previewUrl
                            , mimeType = fileInfo.mimeType
                            , lastModified = fileInfo.lastModified
                            , fileName = fileInfo.fileName
                        });
                    }
                    else
                    {
                        byte[] fileContent = content.Length - 256 - extensionSize > 0 ? new byte[content.Length - 256 - extensionSize] : null;
                        if (fileContent != null)
                        {
                            Array.Copy(content, 256 + extensionSize, fileContent, 0, content.Length - 256 - extensionSize);
                        }
                        if (fileInfo.contentIsJSON)
                        {
                            retVal = fileContent != null ? System.Text.UTF8Encoding.UTF8.GetString(fileContent) : null;
                        }
                        else
                            retVal = jss.Serialize(new FileUploadObj() { 
                                base64 = fileContent != null ? Convert.ToBase64String(fileContent) : null
                                , mimeType = fileInfo.mimeType
                                , lastModified = fileInfo.lastModified
                                , fileName = fileInfo.fileName 
                            });
                    }
                }
                catch (Exception ex)
                {
                    retVal = content != null ? Convert.ToBase64String(content) : null;
                    // never happen, i.e. do nothing just to get around unnecessary compilation warning
                    if (ex == null) return null;
                }
            }
            else
            {
                retVal = content != null ? Convert.ToBase64String(content) : null;
            }

            int.TryParse((string)Application["MaxJsonLength"] ?? "102400", out MaxJsonLength);

            if (checkJSONSize 
                && retVal != null 
                && retVal.Length > MaxJsonLength - 1000)
            {
                // better error tracing instead of standard 500 error code at ASMX level
                throw new Exception(string.Format("Web API cannot handle size of content {0}/{1}, consider increase the limit in <system.web.extensions> section", retVal.Length, MaxJsonLength));
            }
            else
            {
                return retVal;
            }
        }

        protected Tuple<string, byte[]> BlobImage(byte[] content, bool bFullBLOB = false)
        {
            const int maxOriginalSize = 2000;

            Func<byte[], string, bool, Tuple<string, byte[], bool>> tryResizeImage = (ba, mimeType, resize) =>
            {
                try
                {
                    if (!resize) return new Tuple<string, byte[], bool>(mimeType, ba,  false);
                    return new Tuple<string,byte[],bool>("image/jpeg", ResizeImage(ba, 96), true);
                }
                catch
                {
                    if (ba.Length <= maxOriginalSize) 
                        return new Tuple<string, byte[], bool>(mimeType, ba, false);
                    else
                        return new Tuple<string, byte[], bool>(mimeType, new byte[0], true);

                }
            };

            try
            {
                string fileContent = DecodeFileStream(content, bFullBLOB);
                System.Web.Script.Serialization.JavaScriptSerializer jss = new System.Web.Script.Serialization.JavaScriptSerializer();
                jss.MaxJsonLength = Int32.MaxValue;
                try
                {
                    FileUploadObj fileInfo = jss.Deserialize<FileUploadObj>(fileContent);
                    string mimeType = string.IsNullOrEmpty(fileInfo.mimeType) ? "application/base64" : fileInfo.mimeType;
                    var resized =tryResizeImage(Convert.FromBase64String(fileInfo.base64),mimeType, !bFullBLOB);
                    return new Tuple<string, byte[]>(resized.Item1, resized.Item2);
                }
                catch
                {
                    try
                    {
                        List<_ReactFileUploadObj> fileList = jss.Deserialize<List<_ReactFileUploadObj>>(fileContent);
                        List<FileUploadObj> x = new List<FileUploadObj>();
                        foreach (var fileInfo in fileList)
                        {
                            string mimeType = string.IsNullOrEmpty(fileInfo.mimeType) ? "application/base64" : fileInfo.mimeType;
                            var resized = tryResizeImage(Convert.FromBase64String(fileInfo.base64), mimeType, !bFullBLOB);
                            return new Tuple<string, byte[]>(resized.Item1, resized.Item2);
                        }
                        return null;
                    }
                    catch
                    {
                        string mimeType = "image/jpeg";
                        var resized = tryResizeImage(Convert.FromBase64String(fileContent), mimeType, !bFullBLOB);
                        return new Tuple<string, byte[]>(resized.Item1, resized.Item2);
                    }
                }
            }
            catch
            {
                return null;
            }
        }

        protected string BlobPlaceHolder(byte[] content, bool resize = true)
        {
            const int maxOriginalSize = 2000;
            bool blobOnly = false;
            System.Web.Script.Serialization.JavaScriptSerializer jss = new System.Web.Script.Serialization.JavaScriptSerializer();
            jss.MaxJsonLength = Int32.MaxValue;

            Func<byte[], byte[]> tryResizeImage = (ba) =>
            {
                try
                {
                    return ResizeImage(ba,96);
                }
                catch
                {
                    return null;
                }
            };

            Func<string, string, byte[], string> makeInlineSrc = (mimeType, contentBase64, icon) =>
            {
                if (mimeType.Contains("image"))
                {
                    if (icon != null) return "data:application/base64;base64," + Convert.ToBase64String(icon);
                    else if (mimeType.Contains("svg") && (contentBase64 ?? "").Length < maxOriginalSize) return "data:" + mimeType + ";base64," + contentBase64;
                    else return "../images/DefaultImg.png";
                }
                else if (mimeType.Contains("pdf"))
                {
                    return "../images/pdfIcon.png";
                }
                else if (mimeType.Contains("word"))
                {
                    return "../images/wordIcon.png";
                }
                else if (mimeType.Contains("openxmlformats") || mimeType.Contains("excel"))
                {
                    return "../images/ExcelIcon.png";
                }
                else
                {
                    return "../images/fileIcon.png";
                }
            };

            Func<string, string> decodeSingleFile = (string fileContent) =>
            {
                FileUploadObj fileInfo = jss.Deserialize<FileUploadObj>(fileContent);
                byte[] icon = fileInfo.base64 != null && (fileInfo.mimeType ?? "image").Contains("image")
                                ? tryResizeImage(Convert.FromBase64String(fileInfo.base64)) : null;
                if (blobOnly)
                {
                    return makeInlineSrc(fileInfo.mimeType ?? "", fileInfo.base64, icon);
                }
                else return jss.Serialize(new FileUploadObj()
                {
                    icon = icon != null
                        ? Convert.ToBase64String(icon)
                        : ((fileInfo.mimeType ?? "").Contains("svg") && (fileInfo.base64 ?? "").Length <= maxOriginalSize ? fileInfo.base64 
                        : null),
                    mimeType = fileInfo.mimeType,
                    lastModified = fileInfo.lastModified,
                    fileName = fileInfo.fileName
                });
            };

            Func<string, string> decodeFileList = (string fileContent) =>
            {
                List<_ReactFileUploadObj> fileList = jss.Deserialize<List<_ReactFileUploadObj>>(fileContent);
                List<FileUploadObj> x = new List<FileUploadObj>();
                foreach (var fileInfo in fileList)
                {
                    byte[] icon = fileInfo.base64 != null && (fileInfo.mimeType ?? "image").Contains("image")
                                    ? tryResizeImage(Convert.FromBase64String(fileInfo.base64)) : null;
                    if (blobOnly)
                    {
                        return makeInlineSrc(fileInfo.mimeType ?? "", fileInfo.base64, icon);
                    }
                    x.Add(new FileUploadObj()
                    {
                        icon = icon != null
                            ? Convert.ToBase64String(icon)
                            : ((fileInfo.mimeType ?? "").Contains("svg") && (fileInfo.base64 ?? "").Length <= maxOriginalSize ? fileInfo.base64 : null),
                        mimeType = fileInfo.mimeType,
                        lastModified = fileInfo.lastModified,
                        fileName = fileInfo.fileName
                    });
                }
                if (blobOnly && fileList.Count == 0) return null;
                else return jss.Serialize(x);
            };

            Func<string, string> decodeRawFile = (string fileContent) =>
            {
                byte[] icon = tryResizeImage(Convert.FromBase64String(fileContent));
                if (blobOnly) return "data:application/base64;base64," + Convert.ToBase64String(icon);
                else return jss.Serialize(new List<FileUploadObj>() { 
                            new FileUploadObj() { 
                                icon = icon != null ? Convert.ToBase64String(icon) : null, 
                                mimeType = "image/jpeg", 
                                fileName = "image" } });
            };

            try
            {
                if (content == null || content.Length == 0) return null;

                string fileContent = DecodeFileStream(content, false, true);
                try
                {
                    if ((fileContent ?? "").Length > 0)
                    {
                        if (fileContent.StartsWith("{"))
                        {
                            return decodeSingleFile(fileContent);
                        }
                        else if (fileContent.StartsWith("["))
                        {
                            return decodeFileList(fileContent);
                        }
                        else
                        {
                            return decodeRawFile(fileContent);
                        }
                    }
                    else return null;

                    FileUploadObj fileInfo = jss.Deserialize<FileUploadObj>(fileContent);
                    byte[] icon = tryResizeImage(Convert.FromBase64String(fileInfo.base64));
                    return jss.Serialize(new FileUploadObj() {
                        icon = icon != null
                            ? Convert.ToBase64String(icon)
                            : (fileInfo.mimeType.Contains("svg") && (fileInfo.base64 ?? "").Length <= maxOriginalSize ? fileInfo.base64 : null),
                        mimeType = fileInfo.mimeType, 
                        lastModified = fileInfo.lastModified, 
                        fileName = fileInfo.fileName });
                }
                catch {
                    try
                    {
                        List<_ReactFileUploadObj> fileList = jss.Deserialize<List<_ReactFileUploadObj>>(fileContent);
                        List<FileUploadObj> x = new List<FileUploadObj>();
                        foreach (var fileInfo in fileList)
                        {
                            byte[] icon = tryResizeImage(Convert.FromBase64String(fileInfo.base64));
                            x.Add(new FileUploadObj() {
                                icon = icon != null
                                    ? Convert.ToBase64String(icon)
                                    : (fileInfo.mimeType.Contains("svg") && (fileInfo.base64 ?? "").Length <= maxOriginalSize ? fileInfo.base64 : null),
                                mimeType = fileInfo.mimeType, 
                                lastModified = fileInfo.lastModified, 
                                fileName = fileInfo.fileName });
                        }
                        return jss.Serialize(x);
                    }
                    catch
                    {
                        byte[] icon = tryResizeImage(Convert.FromBase64String(fileContent));
                        return jss.Serialize(new List<FileUploadObj>() { 
                            new FileUploadObj() { 
                                icon = icon != null ? Convert.ToBase64String(icon) : null, 
                                mimeType = "image/jpeg", 
                                fileName = "image" }});
                    }
                }
            }
            catch {
                return null;
            }
        }

        protected List<SerializableDictionary<string, string>> DataTableToListOfObject(DataTable dt, IncludeBLOB includeBLOB = IncludeBLOB.Icon, Dictionary<string, DataRow> colAuth = null, HashSet<string> utcColumns = null, List<string> colList = null)
        {

            //var x = dt.AsEnumerable().Select(
            //        row => SerializableDictionary<string, string>.CreateInstance(dt.Columns.Cast<DataColumn>().ToDictionary(
            //                column => column.ColumnName,
            //                column => row[column].ToString()
            //            ))
            //    );
            List<SerializableDictionary<string, string>> ret = new List<SerializableDictionary<string, string>>();
            if (dt == null) return ret;
            bool isDtlTbl = GetScreenId() > 0 && dt.Columns.Contains(GetDtlKeyColumnName(false));
            bool isMstTbl = GetScreenId() > 0 && !isDtlTbl && dt.Columns.Contains(GetMstKeyColumnName(false));
            string tableId = isDtlTbl ? 
                                GetDtlTableName(true).Replace(GetDtlTableName(false),"")
                                : (isMstTbl ? GetMstTableName(true).Replace(GetMstTableName(false),"") 
                                : ""
                                );
            string tableName = GetScreenId() > 0 ? (isDtlTbl ? GetDtlTableName(true) : (isMstTbl ? GetMstTableName(true) : "")) : null;
            string keyColumnName = GetScreenId() > 0 ? (isDtlTbl ? GetDtlKeyColumnName(false) : (isMstTbl ? GetMstKeyColumnName(false) : "")) : "";
            string keyColumnUnderlyingName = GetScreenId() > 0 ? (isDtlTbl ? GetDtlKeyColumnName(true) : (isMstTbl ? GetMstKeyColumnName(true) : "")) : "";
            Regex rxBaseCol = new Regex(tableId + "$");
            Func<string, DataRow, string> convertByteArrayToString = (columnName, dr) =>
            {
                // technically wrong as there should be associating of colAuth with direct DB retrieval on content type, FIXME
                string displayMode = colAuth == null || !colAuth.ContainsKey(columnName) 
                                        ? "ImageButton" 
                                        : colAuth[columnName]["DisplayMode"].ToString();
                if (displayMode == "Signature")
                {
                    switch (includeBLOB)
                    {
                        case IncludeBLOB.None:
                            return "data:image/png;base64," + Convert.ToBase64String(dr[columnName] as byte[]);
                            //return null;
                        case IncludeBLOB.Icon:
                            return "data:image/png;base64," + Convert.ToBase64String(dr[columnName] as byte[]);
                            //return RO.Common3.Utils.BlobPlaceHolder(dr[columnName] as byte[]);
                        case IncludeBLOB.Content: 
                            return "data:image/png;base64," + Convert.ToBase64String(dr[columnName] as byte[]);
                    }
                    return  includeBLOB == IncludeBLOB.None 
                                            ? null 
                                            : "data:image/png;base64," + Convert.ToBase64String(dr[columnName] as byte[]);
                }
                else
                {
                    // assume to be ImageButton;
                    if (includeBLOB == IncludeBLOB.None)
                        return null;
                    else if (includeBLOB == IncludeBLOB.DownloadLink 
                            && !string.IsNullOrEmpty(tableName)
                            && (
                            (!string.IsNullOrEmpty(tableId) && rxBaseCol.IsMatch(columnName))
                            ||
                            GetDdlContext().ContainsKey(columnName)
                            )
                            )
                    {
                        var keyId = dr[keyColumnName].ToString();
                        var baseColName = rxBaseCol.Replace(columnName,"");
                        if (GetDdlContext().ContainsKey(columnName))
                        {
                            // pull up image from another table
                            var ddlContext = GetDdlContext();
                            tableName = ddlContext[columnName]["baseTbl"];
                            keyColumnUnderlyingName = ddlContext[columnName]["baseKeyCol"];
                            baseColName = ddlContext[columnName]["baseColName"];
                            keyId = dr[ddlContext[columnName]["mKey"]].ToString();
                        }
                        string url = "~/Dnload.aspx?"
                                + "tbl=" + tableName
                                + "&" + "knm=" + keyColumnUnderlyingName
                                + "&" + "key=" + keyId
                                + "&" + "col=" + baseColName
                                + "&" + "sys=" + GetSystemId()
                                ;
                        return GetUrlWithQSHashV2(url);
                    }
                    else if (includeBLOB == IncludeBLOB.Content || ((byte[])(dr[columnName])).Length < 256)
                        return DecodeFileStream((byte[])(dr[columnName]), true);
                    else if (includeBLOB == IncludeBLOB.Icon)
                        return BlobPlaceHolder((byte[])(dr[columnName]));
                    else if (includeBLOB == IncludeBLOB.DownloadLink && string.IsNullOrEmpty(tableName)) {
                        // inplace link instead if not table backed
                        return RO.Common3.Utils.BlobPlaceHolder(dr[columnName] as byte[], true);
                        //var x = BlobImage(dr[columnName] as byte[], true);
                        //return "data:" + x.Item1 + ";base64," + Convert.ToBase64String(dr[columnName] as byte[]);
                    }
                    else return null;
                }
            }
            ;
            foreach (DataRow dr in dt.Rows)
            {
                SerializableDictionary<string, string> rec = new SerializableDictionary<string, string>();
                foreach (DataColumn col in dt.Columns)
                {                
                    var columnName = col.ColumnName;
                    var colType = dr[columnName].GetType();

                    if (colList != null && !colList.Contains(columnName)) continue;
                    if (colAuth == null
                        || columnName == GetMstKeyColumnName()
                        || columnName == GetDtlKeyColumnName()
                        || (colAuth.ContainsKey(columnName) && colAuth[columnName]["ColVisible"].ToString() != "N")
                        || (colAuth.ContainsKey(columnName + "Text") && colAuth[columnName + "Text"]["ColVisible"].ToString() != "N")
                        )
                        rec[columnName] =
                            colType == typeof(DateTime) ? (((DateTime)dr[columnName]).ToString("o") + (utcColumns == null || !utcColumns.Contains(columnName) ? "" : "Z")) :
                            colType == typeof(byte[]) ? 
                                (dr[columnName] != null
                                    ? convertByteArrayToString(columnName, dr) 
                                    : null) 
                            : dr[columnName].ToString()
                            ;
                    else rec[columnName] = null;
                }
                ret.Add(rec);
            }
            return ret;
        }

        protected List<SerializableDictionary<string, string>> DataTableToListOfObject(DataTable dt, bool includeBLOB, Dictionary<string, DataRow> colAuth)
        {
            return DataTableToListOfObject(dt, includeBLOB ? IncludeBLOB.Content : IncludeBLOB.Icon, colAuth);
        }

        protected List<SerializableDictionary<string, string>> DataTableToListOfDdlObject(DataRowView drCri, DataTable dt)
        {

            //var x = dt.AsEnumerable().Select(
            //        row => SerializableDictionary<string, string>.CreateInstance(dt.Columns.Cast<DataColumn>().ToDictionary(
            //                column => column.ColumnName,
            //                column => row[column].ToString()
            //            ))
            //    );

            string keyColumn = drCri["DdlKeyColumnName"].ToString();
            string refColumn = drCri["DdlRefColumnName"].ToString();
            int idx = 0;

            List<SerializableDictionary<string, string>> ret = new List<SerializableDictionary<string, string>>();
            foreach (DataRow dr in dt.Rows)
            {
                SerializableDictionary<string, string> rec = new SerializableDictionary<string, string>();

                rec.Add("key", dr[keyColumn].ToString());
                rec.Add("label", dr[refColumn].ToString());
                rec.Add("value", dr[keyColumn].ToString());
                rec.Add("idx", idx.ToString());
                ret.Add(rec);

                idx++;
            }
            return ret;
        }

        protected SerializableDictionary<string, SerializableDictionary<string, string>> DataTableToLabelObject(DataTable dt, List<string> keyColumns)
        {
            var ret = new SerializableDictionary<string, SerializableDictionary<string, string>>();
            int tabIndex = 10;
            foreach (DataRow dr in dt.Rows)
            {
                var rec = new SerializableDictionary<string, string>();
                foreach (DataColumn col in dt.Columns)
                {
                    rec[col.ColumnName] = dr[col.ColumnName].ToString();
                }
                rec["TabIndex"] = tabIndex.ToString();
                ret.Add(GetKeyValue(dr, keyColumns), rec);
                tabIndex += 10;
            }
            return ret;
        }

        protected string GetDVFilterExpression(string sFind, DataTable dtAuth, string filteredColName, bool isMaster)
        {
            List<string> filterExpression = new List<string>();
            if (dtAuth != null)
            {
                foreach (DataRow dr in dtAuth.Rows)
                {
                    string sExpression = string.Empty;
                    if (
                        (isMaster && dr["MasterTable"].ToString().ToUpper() != "Y")
                        ||
                        (!isMaster && dr["MasterTable"].ToString().ToUpper() != "N")
                        ||
                        dr["ColVisible"].ToString().ToUpper() != "Y"
                        ||
                        (!string.IsNullOrEmpty(filteredColName) && filteredColName != dr["ColName"].ToString())
                        ) 
                        continue;

                    if (dr["DisplayMode"].ToString().Contains("Date"))
                    {
                        DateTime searchDate;
                        bool isDate = DateTime.TryParse(sFind, out searchDate);

                        if (isDate)
                        {
                            sExpression = string.Format(System.Globalization.CultureInfo.InvariantCulture.DateTimeFormat, "{0} = #{1}#", dr["ColName"].ToString(), searchDate);
                        }
                        else
                        {
                            sExpression = string.Format("convert({0},'System.String') like '*{1}*'", dr["ColName"].ToString(), sFind.Replace("*", string.Empty).Replace("%", string.Empty).Replace("'", "''"));
                        }
                    }
                    else
                    {
                        sExpression = "convert(" + dr["ColName"].ToString() + ",'System.String') like '*" + sFind.Replace("*", string.Empty).Replace("%", string.Empty).Replace("'", "''") + "*'";
                    }
                    filterExpression.Add(sExpression);
                    if (filteredColName == dr["ColName"].ToString()) break;
                }
            }
            return string.Join(" OR ", filterExpression.ToArray());
        }

        protected virtual AutoCompleteResponse GridLisSuggests(DataTable dtSuggest, int topN, Dictionary<string,string> options, bool isMaster)
        {
            AutoCompleteResponse r = new AutoCompleteResponse();
            DataTable dtColAuth = _GetAuthCol(GetScreenId());
            DataTable dtColLabel = _GetScreenLabel(GetScreenId());
            Dictionary<string, DataRow> colAuth = dtColAuth.AsEnumerable().ToDictionary(dr => dr["ColName"].ToString());
            var utcColumnList = dtColLabel.AsEnumerable().Where(dr => dr["DisplayMode"].ToString().Contains("UTC")).Select(dr => dr["ColumnName"].ToString() + dr["TableId"].ToString()).ToArray();
            HashSet<string> utcColumns = new HashSet<string>(utcColumnList);
            Dictionary<string, string> listPosMap = new Dictionary<string, string>();
            var ddlContext = GetDdlContext();
            string mstKeyColumnName = GetMstKeyColumnName();
            string sortColumn = options != null && options.ContainsKey("_SortColumn") ? options["_SortColumn"] : "";
            string filterColumn = options != null && options.ContainsKey("_FilterColumn") ? options["_FilterColumn"] : "";
            string filterValue = options != null && options.ContainsKey("_FilterValue") ? options["_FilterValue"] : "";
            string filterExpression = string.IsNullOrEmpty(filterValue) ? "" : GetDVFilterExpression(filterValue, dtColAuth, filterColumn, isMaster);
            List<string> listColumns = dtColLabel.AsEnumerable()
                .Select(dr =>
                {
                    var pos = dr["DtlLstPosId"].ToString();
                    var columnName = dr["ColumnName"].ToString() + dr["TableId"].ToString();

                    if (
                        !string.IsNullOrEmpty(pos)
                        ||
                        columnName == GetMstKeyColumnName()
                        )
                    {
                        columnName = columnName + (ddlContext.ContainsKey(columnName) && columnName != mstKeyColumnName ? "Text" : "");
                        if (!string.IsNullOrEmpty(pos)) listPosMap[columnName] = pos;
                        return columnName;
                    }
                    else return null;
                })
                .Where(s => !string.IsNullOrEmpty(s)).ToList();
            DataTable dt = dtSuggest.Clone();
            DataView dv = new DataView(dtSuggest);
            dv.Sort = sortColumn;
            dv.RowFilter = filterExpression;

            int ii = 0;
            foreach (DataRowView drv in dv)
            {
                dt.ImportRow(drv.Row);
                ii = ii + 1;
                if (ii >= topN && topN != 0) break;
            }
            r.data = DataTableToListOfObject(dt, IncludeBLOB.Icon, colAuth, utcColumns, listColumns)
                        .Select(d =>
                        {
                            SerializableDictionary<string, string> x = new SerializableDictionary<string, string>();
                            foreach (var y in d.Keys)
                            {
                                if (!listPosMap.ContainsKey(y))
                                {
                                    if (y == mstKeyColumnName)
                                    {
                                        x["key"] = d[y];
                                        x["value"] = d[y];
                                    }
                                    else
                                        x[y] = d[y];
                                }
                                else
                                {
                                    var pos = listPosMap[y];
                                    if (pos == "1") x["label"] = d[y];
                                    else if (pos == "2") x["detail"] = d[y];
                                    else if (pos == "3") x["labelR"] = d[y];
                                    else if (pos == "4") x["detailR"] = d[y];
                                    else x[y] = d[y];
                                }
                            }
                            return x;
                        })
                        .ToList();
            r.query = "";
            r.topN = topN;
            r.total = dv.Count;
            r.matchCount = dv.Count;
            return r;
        }
        private string GetKeyValue(DataRow dr, List<string> keyColumns)
        {
            string key = string.Empty;
            foreach(string column in keyColumns)
            {
                key += dr[column];
            }
            return key;
        }

        protected List<SerializableDictionary<string, string>> DataTableToListOfDdlObject(DataTable dt, string keyColumn, string refColumn, List<string> addtionalColumns)
        {
            //var x = dt.AsEnumerable().Select(
            //        row => SerializableDictionary<string, string>.CreateInstance(dt.Columns.Cast<DataColumn>().ToDictionary(
            //                column => column.ColumnName,
            //                column => row[column].ToString()
            //            ))
            //    );

            int idx = 0;
            var x = DataTableToListOfObject(dt);
            List<SerializableDictionary<string, string>> ret = new List<SerializableDictionary<string, string>>();
            foreach (var o in x)
            {
                SerializableDictionary<string, string> rec = new SerializableDictionary<string, string>();

                rec.Add("key", o[keyColumn].ToString());
                rec.Add("label", string.IsNullOrEmpty(o[refColumn]) ? " " : o[refColumn]); // react doesn't like empty label
                rec.Add("value", o[keyColumn].ToString());
                rec.Add("idx", idx.ToString());

                foreach(string column in addtionalColumns)
                {
                    rec.Add(column, o[column].ToString());
                }

                ret.Add(rec);

                idx++;
            }
            return ret;
        }
        protected ApiResponse<AutoCompleteResponseObj, SerializableDictionary<string, AutoCompleteResponseObj>> DataTableToLabelResponse(DataTable dt, List<string> keyColumns)
        {
            ApiResponse<AutoCompleteResponseObj, SerializableDictionary<string, AutoCompleteResponseObj>> mr = new ApiResponse<AutoCompleteResponseObj, SerializableDictionary<string, AutoCompleteResponseObj>>();
            AutoCompleteResponseObj r = new AutoCompleteResponseObj();
            SerializableDictionary<string, string> result = new SerializableDictionary<string, string>();
            mr.errorMsg = "";
            r.data = DataTableToLabelObject(dt, keyColumns);
            r.query = "";
            r.topN = 0;
            r.total = dt.Rows.Count;
            mr.data = r;
            mr.status = "success";
            return mr;
        }

        protected ApiResponse<AutoCompleteResponse, SerializableDictionary<string, AutoCompleteResponse>> DataTableToApiResponse(DataTable dt, string query, int topN)
        {
            ApiResponse<AutoCompleteResponse, SerializableDictionary<string, AutoCompleteResponse>> mr = new ApiResponse<AutoCompleteResponse, SerializableDictionary<string, AutoCompleteResponse>>();
            AutoCompleteResponse r = new AutoCompleteResponse();
            SerializableDictionary<string, string> result = new SerializableDictionary<string, string>();
            mr.errorMsg = "";
            r.data = DataTableToListOfObject(dt);
            r.query = query;
            r.topN = topN;
            r.total = dt.Rows.Count;
            mr.data = r;
            mr.status = "success";
            return mr;
        }

        protected ApiResponse<AutoCompleteResponse, SerializableDictionary<string, AutoCompleteResponse>> DataTableToDdlApiResponse(DataTable dt, string keyColumn, string refColumn)
        {
            return DataTableToDdlApiResponse(dt, keyColumn, refColumn, new List<string>() { });
        }

        protected ApiResponse<AutoCompleteResponse, SerializableDictionary<string, AutoCompleteResponse>> DataTableToDdlApiResponse(DataTable dt, string keyColumn, string refColumn, List<string> additionalColumns)
        {
            ApiResponse<AutoCompleteResponse, SerializableDictionary<string, AutoCompleteResponse>> mr = new ApiResponse<AutoCompleteResponse, SerializableDictionary<string, AutoCompleteResponse>>();
            AutoCompleteResponse r = new AutoCompleteResponse();
            SerializableDictionary<string, string> result = new SerializableDictionary<string, string>();
            mr.errorMsg = "";
            r.data = DataTableToListOfDdlObject(dt, keyColumn, refColumn, additionalColumns);
            mr.data = r;
            mr.status = "success";
            return mr;
        }

        protected string GetValueOrDefault(SerializableDictionary<string, string> options, string buttonName, string dflt)
        {
            return options.ContainsKey(buttonName) ? options[buttonName] : dflt;
        }
        protected string GetCriteriaLs(ArrayList values)
        {
            string result = string.Empty;

            if (values == null)
            {
                return result;
            }

            foreach (string val in values)
            {
                if (!string.IsNullOrEmpty(val))
                {
                    if (!string.IsNullOrEmpty(result))
                    {
                        result += ",";
                    }

                    result += "'" + val + "'";
                }
            }

            if (!string.IsNullOrEmpty(result))
            {
                result = "(" + result + ")";
            }

            return result;
        }

        private List<MenuNode> BuildMenuTree(HashSet<string> mh, DataView dvMenu, string[] path, int level, bool bRecurr)
        {
            List<MenuNode> menus = new List<MenuNode>();
            string selectedQid = string.Join(".", path);
            //string[] subPath = path.Skip<string>(1).ToArray<string>();
            //string[] emptyPath = new string[0];
            if (mh.Count == 0) foreach (DataRow dr in dvMenu.Table.Rows) { mh.Add(dr["MenuId"].ToString()); }

            foreach (DataRowView drv in dvMenu)
            {
                MenuNode mt = new MenuNode
                {
                    ParentQId = drv["ParentQId"].ToString(),
                    ParentId = drv["ParentId"].ToString(),
                    QId = drv["QId"].ToString(),
                    MenuId = drv["MenuId"].ToString(),
                    NavigateUrl = drv["NavigateUrl"].ToString(),
                    QueryStr = drv["QueryStr"].ToString(),
                    IconUrl = drv["IconUrl"].ToString(),
                    Popup = drv["Popup"].ToString(),
                    GroupTitle = drv["GroupTitle"].ToString(),
                    MenuText = drv["MenuText"].ToString(),
                    Selected = selectedQid.StartsWith(drv["QId"].ToString()),
                    Level = level,
                    Children = bRecurr ? BuildMenuTree(mh,new DataView(dvMenu.Table, string.Format("ParentId = {0}", drv["MenuId"].ToString()), "ParentId, ParentQId,Qid", DataViewRowState.CurrentRows), path, level + 1, bRecurr) : new List<MenuNode>()
                };
                if (mt.ParentId == "" || mh.Contains(mt.ParentId)) menus.Add(mt);
            }
            return menus;
        }

        #region app domain resolving helpers
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
                        : new Regex("^" + GetDomainUrl(true), RegexOptions.IgnoreCase).Replace(url, "");
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
            var Request = HttpContext.Current.Request;
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
            return ResolveUrlCustom(url, false, true);
        }
        #endregion
        public AsmxBase()
        {
            var Request = HttpContext.Current.Request;
            IntPageUrlBase = (Request.IsSecureConnection ? "https://" : "http://")
                                + Request.Url.Host
                                + (Request.Url.IsDefaultPort ? "" : ":" + Request.Url.Port.ToString())
                                + (Request.ApplicationPath + "/").Replace("//", "/");
            PageUrlBase = ResolveUrlCustom("~/", false, true);
        }


        protected ApiResponse<List<SerializableDictionary<string, string>>, SerializableDictionary<string, AutoCompleteResponse>> emptyListResponse()
        {
            List<SerializableDictionary<string, string>> content = new List<SerializableDictionary<string, string>>();
            ApiResponse<List<SerializableDictionary<string, string>>, SerializableDictionary<string, AutoCompleteResponse>> mr = new ApiResponse<List<SerializableDictionary<string, string>>, SerializableDictionary<string, AutoCompleteResponse>>();
            mr.data = new List<SerializableDictionary<string, string>>();
            mr.status = "success";
            mr.errorMsg = "";
            return mr;
        }
        protected ApiResponse<AutoCompleteResponse, SerializableDictionary<string, AutoCompleteResponse>> emptyAutoCompleteResponse()
        {
            List<SerializableDictionary<string, string>> content = new List<SerializableDictionary<string, string>>();
            ApiResponse<AutoCompleteResponse, SerializableDictionary<string, AutoCompleteResponse>> mr = new ApiResponse<AutoCompleteResponse, SerializableDictionary<string, AutoCompleteResponse>>();
            AutoCompleteResponse r = new AutoCompleteResponse();
            SerializableDictionary<string, string> result = new SerializableDictionary<string, string>();
            mr.errorMsg = "";
            r.data = content;
            r.query = "";
            r.topN = 0;
            r.total = 0;
            mr.data = r;
            mr.status = "success";
            return mr;
        }
        protected ApiResponse<SaveDataResponse, SerializableDictionary<string, AutoCompleteResponse>> DelMultiDoc(string mstId, string dtlId, bool isMaster, string[] docIdList, string screenColumnName, string columnName, string mstTableName, string ddlKeyTableName, string mstKeyColumnName)
        {
            //screenColumnName = "FirmDoc25";
            //string columnName = "FirmDoc";
            //string mstTableName = "FirmInfo";
            //string ddlKeyTableName = "FirmDoc";
            //string mstKeyColumnName = "FirmInfoId";
            Func<ApiResponse<SaveDataResponse, SerializableDictionary<string, AutoCompleteResponse>>> fn = () =>
            {
                SwitchContext(GetSystemId(), LCurr.CompanyId, LCurr.ProjectId);
                ValidatedMstId(GetValidateMstIdSPName(), GetSystemId(), GetScreenId(), "**" + mstId, MatchScreenCriteria(new DataView(_GetScrCriteria(GetScreenId())), null));
                string tableColumnName = ValidatedColAuth(screenColumnName, isMaster, "Document", null, true);
                List<string> deletedDocId = new List<string>();
                DataTable dtDocList = _GetDocList(mstId, screenColumnName);
                if (string.IsNullOrEmpty(tableColumnName))
                {
                    dtDocList.Clear();
                    dtDocList.AcceptChanges();
                }
                foreach (var docId in docIdList)
                {
                    bool hasDoc = (from x in dtDocList.AsEnumerable() where !string.IsNullOrEmpty(docId) && x["DocId"].ToString() == docId select x).Count() > 0;
                    bool canDeleteMultiDoc = PreDelMultiDoc(mstId, dtlId, isMaster, docId, true, screenColumnName, mstTableName, null, null);
                    if (hasDoc && canDeleteMultiDoc && !string.IsNullOrEmpty(tableColumnName))
                    {
                        (new AdminSystem()).DelDoc(isMaster ? mstId : dtlId, docId, LUser.UsrId.ToString(), ddlKeyTableName, mstTableName, columnName, mstKeyColumnName, LcAppConnString, LcAppPw);
                        PostDelMultiDoc(mstId, dtlId, isMaster, docId, true, screenColumnName, mstTableName, ddlKeyTableName, null);
                        deletedDocId.Add(docId);
                    }
                }

                ApiResponse<SaveDataResponse, SerializableDictionary<string, AutoCompleteResponse>> mr = new ApiResponse<SaveDataResponse, SerializableDictionary<string, AutoCompleteResponse>>();
                System.Web.Script.Serialization.JavaScriptSerializer jss = new System.Web.Script.Serialization.JavaScriptSerializer();
                SaveDataResponse result = new SaveDataResponse()
                {
                    mst = isMaster ? new SerializableDictionary<string, string>() { { "docIdList", jss.Serialize(deletedDocId) } } : null,
                    dtl = !isMaster ? new List<SerializableDictionary<string, string>>() {
                        new SerializableDictionary<string, string>() {{"docIdList", jss.Serialize(deletedDocId)}}
                    } : null
                };
                string msg = "Document(s) Deleted";
                result.message = msg;
                mr.status = "success";
                mr.errorMsg = "";
                mr.data = result;
                return mr;
            };
            var ret = ProtectedCall(RestrictedApiCall(fn, GetSystemId(), GetScreenId(), "S", screenColumnName)); ;
            return ret;
        }
        protected ApiResponse<SaveDataResponse, SerializableDictionary<string, AutoCompleteResponse>> SaveMultiDoc(string mstId, string dtlId, bool isMaster, string docId, bool overwrite, string screenColumnName, string tableName, string docJson, SerializableDictionary<string, string> options)
        {
            Func<ApiResponse<SaveDataResponse, SerializableDictionary<string, AutoCompleteResponse>>> fn = () =>
            {
                SwitchContext(GetSystemId(), LCurr.CompanyId, LCurr.ProjectId);
                ValidatedMstId(GetValidateMstIdSPName(), GetSystemId(), GetScreenId(), "**" + mstId, MatchScreenCriteria(new DataView(_GetScrCriteria(GetScreenId())), null));
                string tableColumnName = ValidatedColAuth(screenColumnName, isMaster, "Document", null, true);
                List<_ReactFileUploadObj> fileArray = DestructureFileUploadObject(docJson);
                DataTable dtDocList = _GetDocList(mstId, screenColumnName);
                bool hasDoc = (from x in dtDocList.AsEnumerable() where x["DocId"].ToString() == docId select x).Count() > 0;
                _ReactFileUploadObj fileObj = fileArray[0];
                // In case DocId has not been saved properly, always find the most recent to replace as long as it has the same file name:
                string DocId = string.Empty;
                byte[] content = Convert.FromBase64String(fileObj.base64);
                DocId = new AdminSystem().GetDocId(isMaster ? mstId : dtlId, tableName, fileObj.fileName, LUser.UsrId.ToString(), LcAppConnString, LcAppPw);
                bool canSaveMultiDoc = PreSaveMultiDoc(mstId, dtlId, isMaster, DocId, overwrite, screenColumnName, tableName, docJson, options);
                if (string.IsNullOrEmpty(tableColumnName) || !canSaveMultiDoc) throw new UnauthorizedAccessException("access denied on add");
                try
                {
                    if (DocId == string.Empty || !overwrite)
                    {
                        DocId = new AdminSystem().AddDbDoc(isMaster ? mstId : dtlId, tableName, fileObj.fileName, fileObj.mimeType, content.Length, content, LcAppConnString, LcAppPw, LUser);
                    }
                    else
                    {
                        new AdminSystem().UpdDbDoc(DocId, tableName, fileObj.fileName, fileObj.mimeType, content.Length, content, LcAppConnString, LcAppPw, LUser);
                    }
                }
                catch (Exception ex)
                {
                    ErrorTracing(new Exception(string.Format("{0}, {1}, {2}-{3}({4})", mstId, docId, fileObj.fileName, fileObj.mimeType), ex));
                    throw;
                }
                PostSaveMultiDoc(mstId, dtlId, isMaster, DocId, overwrite, screenColumnName, tableName, docJson, options);
                ApiResponse<SaveDataResponse, SerializableDictionary<string, AutoCompleteResponse>> mr = new ApiResponse<SaveDataResponse, SerializableDictionary<string, AutoCompleteResponse>>();
                SaveDataResponse result = new SaveDataResponse()
                {
                    mst = isMaster ? new SerializableDictionary<string, string>() { { "DocId", DocId } } : null,
                    dtl = !isMaster ? new List<SerializableDictionary<string, string>>() {
                        new SerializableDictionary<string, string>() {{ "DocId", DocId }}
                    } : null
                };
                string msg = "Document Saved";
                result.message = msg;
                mr.status = "success";
                mr.errorMsg = "";
                mr.data = result;
                return mr;
            };
            var ret = ProtectedCall(RestrictedApiCall(fn, GetSystemId(), GetScreenId(), "S", screenColumnName)); ;
            return ret;
        }
        protected ApiResponse<AutoCompleteResponse, SerializableDictionary<string, AutoCompleteResponse>> GetMultiDocList(string mstId, string dtlId, bool isMaster, string screenColumnName, string getDdlMethod)
        {
            Func<ApiResponse<AutoCompleteResponse, SerializableDictionary<string, AutoCompleteResponse>>> fn = () =>
            {
                SwitchContext(GetSystemId(), LCurr.CompanyId, LCurr.ProjectId);
                var dtScreenCriteria = _GetScrCriteria(GetScreenId());
                var dvScreenCriteria = new DataView(dtScreenCriteria);
                var LastScreenCriteria = GetScreenCriteria();
                var savedScreenCriteria = LastScreenCriteria.data.data;
                var currentScrCriteria = _GetCurrentScrCriteria(dvScreenCriteria, savedScreenCriteria, true);
                ValidatedMstId(GetValidateMstIdSPName(), GetSystemId(), GetScreenId(), "**" + mstId, MatchScreenCriteria(dvScreenCriteria, new JavaScriptSerializer().Serialize(currentScrCriteria.Item2)));
                string tableColumnName = ValidatedColAuth(screenColumnName, isMaster, "Document", null, false);
                DataTable dt = (new AdminSystem()).GetDdl(GetScreenId(), getDdlMethod, false, false, 0, mstId, LcAppConnString, LcAppPw, string.Empty, LImpr, LCurr);
                for (int i = dt.Rows.Count; i < dt.Rows.Count; i++)
                {
                    DataRow dr = dt.Rows[i];
                    if (string.IsNullOrEmpty(tableColumnName))
                        dt.Rows.Remove(dr);
                    else
                    {
                        string url = ResolveUrlCustom(dr["DocLink"].ToString(), false, true);
                        string securedUrl = GetUrlWithQSHashV2(url);
                        dr["DocLink"] = securedUrl;
                    }
                }
                dt.AcceptChanges();
                return DataTableToApiResponse(dt, "", 0);
            };
            var ret = ProtectedCall(RestrictedApiCall(fn, GetSystemId(), GetScreenId(), "R", screenColumnName, emptyAutoCompleteResponse), AllowAnonymous());
            return ret;
        }
        protected LoadScreenPageResponse _GetScreenMetaData(bool accessControlled, List<string> includedList = null)
        {
            int screenId = GetScreenId();
            byte systemId = GetSystemId();
            string custLabelCat = new Regex(screenId.ToString() + "$").Replace(GetProgramName(), "");
            // MUST DO THIS FIRST as it would switch system context
            var systemLabels = includedList == null || includedList.Contains("SystemLabels") ? GetSystemLabels("cSystem") : null;
            // now we switch back to our own context
            SwitchContext(systemId, LCurr.CompanyId, LCurr.ProjectId, accessControlled, false, false, true);
            var dtAuthCol = includedList == null || includedList.Contains("AuthCol") ? _GetAuthCol(screenId) : null;
            var dtAuthRow = includedList == null || includedList.Contains("AuthRow") ? _GetAuthRow(screenId) : null;
            var dtScreenLabel = includedList == null || includedList.Contains("ScreenLabel") ? _GetScreenLabel(screenId) : null;
            var dtScreenCriteria = includedList == null || includedList.Contains("ScreenCriteria") ? _GetScrCriteria(screenId) : null;
            var dtScreenFilter = includedList == null || includedList.Contains("ScreenFilter") ? _GetScreenFilter(screenId) : null;
            var dtScreenHlp = includedList == null || includedList.Contains("ScreenHlp") ? _GetScreenHlp(screenId) : null;
            var dtScreenButtonHlp = includedList == null || includedList.Contains("ScreenButtonHlp") ? _GetScreenButtonHlp(screenId) : null;
            var dtLabel = includedList == null || includedList.Contains("Labels") ? _GetLabels(custLabelCat) : null;
            LoadScreenPageResponse result = new LoadScreenPageResponse()
            {
                AuthCol = DataTableToListOfObject(dtAuthCol),
                AuthRow = DataTableToListOfObject(dtAuthRow),
                ColumnDef = DataTableToListOfObject(dtScreenLabel),
                Label = DataTableToListOfObject(dtLabel),
                ScreenButtonHlp = DataTableToListOfObject(dtScreenButtonHlp),
                ScreenCriteria = DataTableToListOfObject(dtScreenCriteria),
                ScreenFilter = DataTableToListOfObject(dtScreenFilter),
                ScreenHlp = DataTableToListOfObject(dtScreenHlp),
                SystemLabels = systemLabels == null ? new List<SerializableDictionary<string,string>>() : systemLabels.data.data,
            };
            return result;
        }

        protected SerializableDictionary<string, List<SerializableDictionary<string, string>>> _GetScreeDdls(bool accessControlled, List<string> includedList = null)
        {
            int screenId = GetScreenId();
            byte systemId = GetSystemId();
            SwitchContext(systemId, LCurr.CompanyId, LCurr.ProjectId, accessControlled, false, false, true);
            Dictionary<string, DataRow> dtMenuAccess = GetScreenMenu(systemId, screenId);
            DataTable dtAuthRow = _GetAuthRow(screenId);
            DataTable dtAuthCol = _GetAuthCol(screenId);
            Dictionary<string, DataRow> authCol = dtAuthCol.AsEnumerable().ToDictionary(dr => dr["ColName"].ToString());

            var ddlContext = GetDdlContext();
            var Ddl = new SerializableDictionary<string, List<SerializableDictionary<string, string>>>();

            foreach (var x in ddlContext.Select((context)=>{
                if (
                    (includedList == null || includedList.Contains(context.Key))
                    &&
                    (!accessControlled || _AllowScreenColumnAccess(screenId, context.Key, "R", dtMenuAccess, dtAuthRow, authCol))
                    )
                {
                    bool bAll = true;
                    bool bAddNew = true;
                    bool bAutoComplete = context.Value.ContainsKey("autocomplete") && context.Value["autocomplete"] == "Y";
                    string keyId = "";
                    DataTable dt = (new AdminSystem()).GetDdl(screenId, context.Value["method"], bAddNew, bAll, bAutoComplete ? 50 : 0, keyId, LcAppConnString, LcAppPw, string.Empty, LImpr, LCurr);
                    return new KeyValuePair<string, List<SerializableDictionary<string, string>>>(context.Key, DataTableToListOfObject(dt));
                }
                else
                {
                    return new KeyValuePair<string, List<SerializableDictionary<string, string>>>(context.Key, new List<SerializableDictionary<string, string>>());
                }
            })) 
            {
                Ddl[x.Key] = x.Value;
            }
            return Ddl;
        }
        protected bool _SetEffectiveScrCriteria(SerializableDictionary<string, string> desiredScreenCriteria)
        {
            var LastScreenCriteria = GetScreenCriteriaEX(true);
            var dtScreenCriteria = _GetScrCriteria(GetScreenId());
            Func<SerializableDictionary<string, string>, SerializableDictionary<string, SerializableDictionary<string, string>>> fn = (d) =>
            {
                if (d == null) return null;
                SerializableDictionary<string, SerializableDictionary<string, string>> x = new SerializableDictionary<string, SerializableDictionary<string, string>>();
                foreach (var o in d)
                {
                    x[o.Key] = new SerializableDictionary<string, string>() { { "LastCriteria", o.Value } };
                }
                return x;
            };
            var filledScrCriteria = fn(desiredScreenCriteria);
            var currentScrCriteria = _GetCurrentScrCriteria(new DataView(dtScreenCriteria), filledScrCriteria ?? LastScreenCriteria.data.data, true);
            var effectiveScrCriteria = _SetCurrentScrCriteria(currentScrCriteria.Item1);
            bool validFilledScrCriteria = (filledScrCriteria.Where((o, i) => {
                return effectiveScrCriteria[i] == o.Value["LastCriteria"]; 
            }).Count() == filledScrCriteria.Count);
            return validFilledScrCriteria;
        }

        protected virtual DataTable _GetDocList(string mstId, string screenColumnName)
        {
            throw new NotImplementedException("Must implement _GetDocList");
        }
        protected virtual string _GetDocTableName(string screenColumnName)
        {
            throw new NotImplementedException("Must implement _GetDocTableName");
        }
        protected string ValidatedDdlValue(string columnName, Dictionary<string, string> refRecord, Dictionary<string, string> curr, Dictionary<string, string> refMst, bool isMultiValueType)
        {
            var ddlContext = GetDdlContext();
            if (ddlContext == null)
            {
                throw new Exception(string.Format("must define proper Ddl context {0} {1}", GetScreenId(), GetSystemId()));
            }
            else if (!ddlContext.ContainsKey(columnName)) {
                throw new Exception(string.Format("must define proper Ddl context for {0} {1} {2}", GetScreenId(), GetSystemId(), columnName));
            }
            var context = ddlContext[columnName];
            var val = curr.ContainsKey(columnName) ? curr[columnName] : (refRecord.ContainsKey(columnName) ? refRecord[columnName] : "");
            var refVal = context.ContainsKey("refCol") && (context["refColSrc"] == "Mst" ? refMst : curr).ContainsKey(context["refColSrcName"]) ? (context["refColSrc"] == "Mst" ? refMst : curr)[context["refColSrcName"]] : null;
            var x = ddlContext[columnName].Clone(new Dictionary<string, string>() { { "refColVal", refVal }, { "addNew", "N" } });
            //var rec = ddlSuggests("**" + val, x, 1);
            //return rec.data.Count > 0 ? rec.data[0]["key"] : "";
            string[] DesiredKeys = (val ?? "").Replace("(", "").Replace(")", "").Replace("undefined", "").Replace("null", "").Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            string SelectedVal = (isMultiValueType ? "(" + string.Join(",", DesiredKeys) + ")" : val);
            var rec = ddlSuggests(DesiredKeys.Length > 0 ? "**" + SelectedVal : "", x, DesiredKeys.Length > 0 ? DesiredKeys.Length : 1);
            return (rec.data.Count > 0 && DesiredKeys.Length > 0) ? (isMultiValueType ? "(" + string.Join(",", rec.data.Select(o => o["key"]).ToArray()) + ")" : rec.data[0]["key"]) : "";
        }
        protected void ValidateField(DataRow drAuth, DataRow drLabel, Dictionary<string, string> refRecord, Dictionary<string,string> defRefRecord, ref SerializableDictionary<string, string> revisedRecord, SerializableDictionary<string, string> refMaster, ref List<KeyValuePair<string, string>> errors, SerializableDictionary<string, string> skipValidation)
        {
            string[] isDdlType = { "ComboBox", "DropDownList", "ListBox", "RadioButtonList" };
            string[] isMultiValueType = { "ListBox" };
            string[] isSimpleTextType = { "TextBox", "MultiLine", "Money", "Currency"};
            string[] isStringDataType = { "10","11","14","15"};
            string[] isBlobType = { "8", "9" };
            string[] isDateType = { "12", "21" };
            string colName = drLabel["ColumnName"].ToString() + drLabel["TableId"].ToString();
            string displayName = drLabel["DisplayName"].ToString();
            string displayMode = drLabel["DisplayMode"].ToString();
            // not sure when this will disappear FIXME
            string colLength = drLabel.Table.Columns.Contains("ColumnLength") ? drLabel["ColumnLength"].ToString() : "999999";
            string dataType = drLabel.Table.Columns.Contains("DataType") ? drLabel["DataType"].ToString() : "";
            bool colVisible = drAuth["ColVisible"].ToString() == "Y";

            // these must not be replaced by db value as they may not be complete from ref record
            if (displayMode == "Document") return;

            string oldVal = refRecord.ContainsKey(colName) ? refRecord[colName] : null;
            string val = revisedRecord.ContainsKey(colName) ? revisedRecord[colName] ?? "" : oldVal;
            string defaultValue = defRefRecord.ContainsKey(colName) ? defRefRecord[colName] : null;
            bool isMasterTable = drAuth["MasterTable"].ToString() == "Y";

            // comma delimited rules to skip
            string skippedValidation = skipValidation != null ? skipValidation[colName] ??  "" : "";
            string skipAllValidation = skipValidation != null ? skipValidation[(isMasterTable ? "SkipAllMst" : "SkipAllDtl")] ?? "" : "";
            if (
                ((drAuth["ColReadOnly"].ToString() == "Y" 
                && !skippedValidation.Contains("ColReadOnly") 
                && !skipAllValidation.Contains("ColReadOnly")) 
                || (
                    drAuth["ColVisible"].ToString() == "N") 
                    && !skippedValidation.Contains("ColVisible") 
                    && !skipAllValidation.Contains("ColVisible"))
                && (
                    oldVal != val
                    )
                )
            {
                /* this should either bounce or use refRecord value FIXME */
                var revertedVal = !string.IsNullOrEmpty(oldVal) ? oldVal : defaultValue ;
                if (colVisible
                    ||
                    (!colVisible 
                        && !string.IsNullOrEmpty(val) 
                        && (!string.IsNullOrEmpty(oldVal) || (val != defaultValue))
                        ))
                {
                    // if visible or not visible but forced with some value that doesn't match existing one
                    errors.Add(new KeyValuePair<string, string>(colName, "readonly value cannot be changed" + " " + drLabel["ColumnHeader"].ToString()));
                }
                else
                {
                    // forced into the record for subsequent checking
                    revisedRecord[colName] = revertedVal;
                }
                val = revertedVal;
            }
            if (displayMode == "ImageButton")
            {
                if (!string.IsNullOrEmpty(drLabel["TableId"].ToString())
                    && revisedRecord.ContainsKey(colName)
                    && !string.IsNullOrEmpty(revisedRecord[colName]))
                {
                    try
                    {
                        List<_ReactFileUploadObj> fileArray = DestructureFileUploadObject(val);
                        if (fileArray.Where(f => string.IsNullOrEmpty(f.base64)).Count() > 0
                            && fileArray.Count > 1
                            )
                        {
                            errors.Add(new KeyValuePair<string, string>(colName, "invalid file upload, incomplete content"));
                        }
                    }
                    catch {
                        // bounce
                        errors.Add(new KeyValuePair<string, string>(colName, "invalid file upload format"));
                    }
                }
            }
            else if (isDdlType.Contains(displayName))
            {
                // this would empty out invalidate field selection
                val = ValidatedDdlValue(colName, refRecord, revisedRecord, drAuth["MasterTable"].ToString() == "Y" || IsGridOnlyScreen() ? revisedRecord : refMaster, isMultiValueType.Contains(displayName));
            }
            else if (isSimpleTextType.Contains(displayMode) && isStringDataType.Contains(dataType))
            {
                // textbox, should be trimmed to avoid overflow error, but lack database size(ColumnLength but not in label definition change that FIXME!!!!!);
                int columnLength = 999999;
                bool hasColumnLength = int.TryParse(colLength, out columnLength);
                if (hasColumnLength && columnLength > 0 && !string.IsNullOrEmpty(val) && val.Trim().Length > columnLength && !skippedValidation.Contains("MaxLength"))
                {
                    // bounce
                    errors.Add(new KeyValuePair<string, string>(colName, "content length cannot exceed" + " " + columnLength.ToString()));
                }
                else
                {
                    // trim then silent cut off, columnLength == 0 means unlimited 
                    val = string.IsNullOrEmpty(val) ? val : (hasColumnLength ? val.Trim().Left(columnLength > 0 ? columnLength : 999999) : val.Trim());
                }
            }
            else if (!isStringDataType.Contains(dataType)
                    && !isDateType.Contains(dataType)
                    && !isBlobType.Contains(dataType)
                    && !string.IsNullOrEmpty(val)
                )
            {
                try
                {
                    decimal v = decimal.Parse(val);
                }
                catch
                {
                    // bounce on non-parsable numeric format, not sure how to handle thousand seperator or other
                    // odd cases like money symbol etc. FIXME
                    errors.Add(new KeyValuePair<string, string>(colName, "invalid numeric format"));
                }
            }
            else if (isSimpleTextType.Contains(displayMode)
                    && isDateType.Contains(dataType)
                    && !isBlobType.Contains(dataType)
                    && !string.IsNullOrEmpty(val)
                )
            {
                try
                {
                    DateTime v = DateTime.Parse(val);
                }
                catch
                {
                    // bounce on non-parsable numeric format, not sure how to handle thousand seperator or other
                    // odd cases like money symbol etc. FIXME
                    errors.Add(new KeyValuePair<string, string>(colName, "invalid date/time format"));
                }
            }
            if (drLabel["RequiredValid"].ToString() == "Y" && string.IsNullOrEmpty(val) && !skippedValidation.Contains("RequiredValid") && !skipAllValidation.Contains("RequiredValid"))
            {
                // supplied value assumed to be from user
                if (revisedRecord.ContainsKey(colName) || (string.IsNullOrEmpty(oldVal) && string.IsNullOrEmpty(defaultValue)))
                {
                    errors.Add(new KeyValuePair<string, string>(colName, drLabel["ErrMessage"].ToString() + " " + drLabel["ColumnHeader"].ToString()));
                }
                else val = string.IsNullOrEmpty(oldVal) ? defaultValue : oldVal; // nothing from user and there is existing, bypass check use existing as the value
            }
            if (!string.IsNullOrEmpty(drLabel["MaskValid"].ToString()) && !(new Regex(drLabel["MaskValid"].ToString())).IsMatch(val) && !skippedValidation.Contains("MaskValid"))
            {
                errors.Add(new KeyValuePair<string, string>(colName, drLabel["ErrMessage"].ToString() + " " + drLabel["ColumnHeader"].ToString()));
            }
            /* should include range check too but dtLabel doesn't have that info, needs to be expanded */

            // never use existing ref data for image button, it can be incomplete
            if (displayMode == "ImageButton") return;

            revisedRecord[colName] = val;
        }
        protected List<KeyValuePair<string, string>> ValidateMst(ref SerializableDictionary<string, string> mst, SerializableDictionary<string, string> currentMst, SerializableDictionary<string, string> skipValidation)
        {
            List<KeyValuePair<string, string>> errors = new List<KeyValuePair<string, string>>();

            var screenId = GetScreenId();
            DataTable dtAut = _GetAuthCol(screenId);
            DataTable dtLabel = _GetScreenLabel(screenId);
            var newMst = InitMaster();
            var revisedMst = mst.Clone();
            int ii = 0;

            foreach (DataRow drLabel in dtLabel.Rows)
            {
                DataRow drAuth = dtAut.Rows[ii];
                if (!string.IsNullOrEmpty(drLabel["TableId"].ToString()) && drAuth["MasterTable"].ToString() == "Y")
                {
                    string colName = drAuth["ColName"].ToString();
                    ValidateField(drAuth, drLabel, currentMst, InitMaster(), ref revisedMst, null, ref errors, skipValidation);
                }
                ii = ii + 1;
            }
            mst = revisedMst;
            return errors;
        }
        protected List<List<KeyValuePair<string, string>>> ValidateDtl(SerializableDictionary<string, string> mst, Dictionary<string, SerializableDictionary<string, string>> currDtlList, ref List<SerializableDictionary<string, string>> dtlList, string dtlKeyIdName, SerializableDictionary<string, string> skipValidation)
        {
            List<List<KeyValuePair<string, string>>> errors = new List<List<KeyValuePair<string, string>>>();
            List<SerializableDictionary<string, string>> validatedList = new List<SerializableDictionary<string, string>>();
            var screenId = GetScreenId();
            DataTable dtAut = _GetAuthCol(screenId);
            DataTable dtLabel = _GetScreenLabel(screenId);
            bool isGridOnlyScreen = IsGridOnlyScreen();
            List<string> accessViolation = new List<string>();
            for (int ii = 0; ii < dtlList.Count; ii++)
            {
                var newDtl = isGridOnlyScreen ? InitMaster() : InitDtl();
                var dtl = dtlList[ii];
                var keyId = dtl[dtlKeyIdName];
                var isDelete = dtl["_mode"] == "delete";
                var isAdd = string.IsNullOrEmpty(keyId);
                if (isGridOnlyScreen)
                {
                    HashSet<string> keys = null;
                    if (dtlList.Count > 20)
                    {
                        DataTable dtSuggest = GetLis(GetValidateMstIdSPName(), GetSystemId(), GetScreenId(), "", new List<string>(), "0", "", "N", 0, false);
                        keys = new HashSet<string>();
                        foreach (DataRow dr in dtSuggest.Rows)
                        {
                            var k = dr[dtlKeyIdName].ToString();
                            if (!string.IsNullOrEmpty(k)) keys.Add(k);
                        }
                    }
                    try
                    {
                        if (isAdd || isDelete)
                        {
                            ValidateAction(GetScreenId(), isDelete ? "D" : "A");
                        }
                        if (!string.IsNullOrEmpty(keyId)) 
                        {
                            if (keys != null) 
                            {
                                if (!keys.Contains(keyId)) throw new Exception(string.Format("access denied **", keyId));
                            }
                            else
                            {
                                ValidatedMstId(GetValidateMstIdSPName(), GetSystemId(), GetScreenId(), "**" + keyId, new List<string>(), false);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        accessViolation.Add(string.Format("{0} KeyId: {1} - {2} ", isDelete ? "delete" : isAdd ? "add" : "update",  keyId, ex.Message));
                        continue;
                    }
                }
                if (dtl["_mode"] == "delete" && !string.IsNullOrEmpty(dtl[dtlKeyIdName]))
                {
                    var revisedDtl = newDtl;
                    revisedDtl[dtlKeyIdName] = dtl[dtlKeyIdName];
                    revisedDtl["_mode"] = dtl["_mode"];
                    validatedList.Add(revisedDtl);
                }
                else if (string.IsNullOrEmpty(dtl[dtlKeyIdName]) || dtl["_mode"] != "delete")
                {
                    var currentDtl = string.IsNullOrEmpty(dtl[dtlKeyIdName]) || !currDtlList.ContainsKey(dtl[dtlKeyIdName]) ? newDtl : currDtlList[dtl[dtlKeyIdName]];
                    var revisedDtl = dtl.Clone();
                    var dtlErrors = new List<KeyValuePair<string, string>>();
                    int jj = 0;
                    // non-exist key, treat as new
                    if (!string.IsNullOrEmpty(dtl[dtlKeyIdName]) && !currDtlList.ContainsKey(dtl[dtlKeyIdName])) revisedDtl[dtl[dtlKeyIdName]] = null;
                    foreach (DataRow drLabel in dtLabel.Rows)
                    {
                        DataRow drAuth = dtAut.Rows[jj];
                        if (!string.IsNullOrEmpty(drLabel["TableId"].ToString()) && (drAuth["MasterTable"].ToString() == "N" || IsGridOnlyScreen()))
                        {
                            string colName = drAuth["ColName"].ToString();
                            ValidateField(drAuth, drLabel, currentDtl,InitDtl(), ref revisedDtl, mst, ref dtlErrors, skipValidation);
                        }
                        jj = jj + 1;
                    }
                    if (dtlErrors.Count > 0)
                    {
                        errors.Add(dtlErrors);
                    }
                    validatedList.Add(revisedDtl);
                }
                else if (!string.IsNullOrEmpty(dtl[dtlKeyIdName]) && dtl["_mode"] == "delete")
                {
                    validatedList.Add(dtl);
                }
            }
            dtlList = validatedList;
            return errors;
        }
        protected Tuple<List<KeyValuePair<string, string>>, List<List<KeyValuePair<string, string>>>> ValidateInput(ref SerializableDictionary<string, string> mst, ref List<SerializableDictionary<string, string>> dtlList, DataTable dtMst, DataTable dtDtl, string mstKeyIdName, string dtlKeyIdName, SerializableDictionary<string, string> skipValidation)
        {
            var pid = mst[mstKeyIdName];
            DataTable dtColLabel = _GetScreenLabel(GetScreenId());
            var utcColumnList = dtColLabel.AsEnumerable().Where(dr => dr["DisplayMode"].ToString().Contains("UTC")).Select(dr => dr["ColumnName"].ToString() + dr["TableId"].ToString()).ToArray();
            HashSet<string> utcColumns = new HashSet<string>(utcColumnList);

            var currMst = string.IsNullOrEmpty(pid) || dtMst.Rows.Count == 0 
                        ? InitMaster() 
                        // full content for image field to validator and utc formatted datetime column, as if it is sent in 
                        : DataTableToListOfObject(dtMst,IncludeBLOB.Content, null, utcColumns)[0];

            List<KeyValuePair<string, string>> mstError = ValidateMst(ref mst, currMst, skipValidation);
            List<List<KeyValuePair<string, string>>> dtlError = new List<List<KeyValuePair<string, string>>>();

            if (dtDtl != null)
            {
                // full content for image field to validator and utc formatted datetime column, as if it is sent in 
                var currDtlList = DataTableToListOfObject(dtDtl, IncludeBLOB.Content, null, utcColumns).ToDictionary(dr => dr[dtlKeyIdName].ToString(), dr => dr);
                dtlError = ValidateDtl(mst, currDtlList, ref dtlList, dtlKeyIdName, skipValidation);
            }

            return new Tuple<List<KeyValuePair<string, string>>, List<List<KeyValuePair<string, string>>>>(mstError, dtlError);
        }
        protected string TranslateItem(DataRowCollection rows, string key)
        {
            try
            {
                return rows.Find(key)[1].ToString();
            }
            catch { return "ERR!"; }
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

        // Procedure for foreign currency rate from external source (do not call this repeatedly, use timer to break up the calls):
        protected string GetExtFxRate(string FrISOCurrencySymbol, string ToISOCurrencySymbol)
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
                /* the following is blocked/retired by google */
                /*
                System.Web.Script.Serialization.JavaScriptSerializer jss = new System.Web.Script.Serialization.JavaScriptSerializer();
                System.Text.RegularExpressions.Regex re = new Regex("^[0-9.]+\\s");
                string url = String.Format("https://www.google.com/finance/converter?a={0}&from={1}&to={2}&meta={3}", "1", FrISOCurrencySymbol, ToISOCurrencySymbol, Guid.NewGuid().ToString());
                System.Net.HttpWebRequest request = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(url);
                request.Referer = "http://www.checkmin.com";
                System.Net.HttpWebResponse response = (System.Net.HttpWebResponse)request.GetResponse();
                System.IO.StreamReader sr = new System.IO.StreamReader(response.GetResponseStream());
                var rate = Regex.Matches(sr.ReadToEnd(),"<span class=\"?bld\"?>([0-9.]+)(.*)</span>")[0].Groups[1].Value;
                return rate.Trim().Replace(",", ".");*/

                var context = HttpContext.Current;
                var cache = context.Cache;
                bool isCrypto = FrISOCurrencySymbol == "FTX" || FrISOCurrencySymbol == "ETH" || ToISOCurrencySymbol == "FTX" || ToISOCurrencySymbol == "ETH";
                string cacheKey = "FXRate_" + FrISOCurrencySymbol + "_" + ToISOCurrencySymbol;
                int minutesToCache = isCrypto ? 15 : 60;
                string price = "";
                lock (cache)
                {
                    price = cache[cacheKey] as string;
                }

                if (!string.IsNullOrEmpty(price)) return price;

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
                price = Newtonsoft.Json.Linq.JObject.Parse(jsonString).SelectToken("['data'].['quote'].['" + ToISOCurrencySymbol + "']['price']").ToString();

                lock (cache)
                {
                    if (cache[cacheKey] as string == null)
                    {
                        cache.Insert(cacheKey, price, new System.Web.Caching.CacheDependency(new string[] { }, loginHandle == null ? new string[] { } : new string[] { loginHandle })
                            , System.Web.Caching.Cache.NoAbsoluteExpiration, new TimeSpan(0, minutesToCache, 0), System.Web.Caching.CacheItemPriority.Normal, null);
                    }
                }

                return price.ToString();
            }
            catch (Exception ex) {

                ErrorTracing(new Exception(string.Format("failed to get FX Rate ({0}) - ({1})", FrISOCurrencySymbol, ToISOCurrencySymbol),ex));
                throw ex;
                //return string.Empty; 
            }
        }

        /* helper functions for ASP.NET inline definition(like default value) conversion */
        protected string converDefaultValue(object val)
        {
            if (val is DateTime) return ((DateTime)val).ToString("o");
            else return val == null ? "" : val.ToString();
        }

        protected string convertDefaultValue(object val)
        {
            if (val is DateTime) return ((DateTime)val).ToString("o");
            else return val == null ? "" : val.ToString();
        }

        protected IncludeBLOB GetBlobOption(string blobOption)
        {
            return blobOption == "I" ? IncludeBLOB.Icon : (blobOption == "N" ? IncludeBLOB.None : (blobOption =="C" ? IncludeBLOB.Content : IncludeBLOB.None));
        }

        protected int GetEffectiveScreenFilterId(string filterId, bool isMaster = true)
        {
            DataTable dt = _GetScreenFilter(GetScreenId());
            int effectiveScreenFilterId = 0;
            int firstApplicableScreenFilterId = 0;
            int parsedFilterId = 0;
            bool filterIdIsName = !string.IsNullOrEmpty(filterId) && !int.TryParse(filterId, out parsedFilterId);
            bool hasDtlFilter = false;
            bool hasMstFilter = false;
            if (dt != null && dt.Rows.Count > 0)
            {
                foreach (DataRow dr in dt.Rows) {
                    if (
                        (
                        (dr["ApplyToMst"].ToString() == "Y" && isMaster)
                        ||
                        (dr["ApplyToMst"].ToString() == "N" && !isMaster)
                        )
                        && 
                        (
                        (dr["ScreenFilterId"].ToString() == filterId && !filterIdIsName)
                        ||
                        (dr["FilterName"].ToString() == filterId && filterIdIsName)
                        )
                        )
                    {
                        if (dr["ApplyToMst"].ToString() == "Y") hasMstFilter = true;
                        else if (dr["ApplyToMst"].ToString() == "N") hasDtlFilter = true;

                        int id = (int)dr["ScreenFilterId"];
                        if (firstApplicableScreenFilterId == 0)
                        {
                            firstApplicableScreenFilterId = id;
                        }
                        if (
                            (!filterIdIsName && id.ToString() == filterId)
                            ||
                            (filterIdIsName && dr["FilterName"].ToString() == filterId)
                            ) 
                        {
                            effectiveScreenFilterId = id;
                            break;
                        }
                    }
                }

            }
            return effectiveScreenFilterId != 0 
                ? effectiveScreenFilterId 
                : (isMaster && !hasDtlFilter || !isMaster && !hasMstFilter) ? firstApplicableScreenFilterId : 0;
        }

        #region visible extern service endpoint
        [WebMethod(EnableSession = false)]
        public ApiResponse<AutoCompleteResponse, SerializableDictionary<string, AutoCompleteResponse>> GetAuthRow()
        {
            Func<ApiResponse<AutoCompleteResponse, SerializableDictionary<string, AutoCompleteResponse>>> fn = () =>
            {
                SwitchContext(GetSystemId(), LCurr.CompanyId, LCurr.ProjectId);
                DataTable dt = _GetAuthRow(GetScreenId());
                return DataTableToApiResponse(dt, "", 0);
            };
            var ret = ProtectedCall(RestrictedApiCall(fn, GetSystemId(), GetScreenId(), "R", null), AllowAnonymous());
            return ret;
        }
        [WebMethod(EnableSession = false)]
        public ApiResponse<AutoCompleteResponse, SerializableDictionary<string, AutoCompleteResponse>> GetAuthCol()
        {
            Func<ApiResponse<AutoCompleteResponse, SerializableDictionary<string, AutoCompleteResponse>>> fn = () =>
            {
                SwitchContext(GetSystemId(), LCurr.CompanyId, LCurr.ProjectId);
                DataTable dtAuthCol = _GetAuthCol(GetScreenId());
                DataTable dtLabel = _GetScreenLabel(GetScreenId());
                List<SerializableDictionary<string, string>> x = new List<SerializableDictionary<string, string>>();
                int ii = 0;
                foreach (DataRow dr in dtAuthCol.Rows)
                {
                    SerializableDictionary<string, string> rec = new SerializableDictionary<string, string>();
                    foreach (DataColumn col in dtAuthCol.Columns)
                    {
                        // normalize naming as AuthCol return '*Text' for dropdown items which is not the convention used in other places
                        rec[col.ColumnName] = col.ColumnName == "ColName" ? dtLabel.Rows[ii]["ColumnName"].ToString() + dtLabel.Rows[ii]["TableId"].ToString() : dr[col.ColumnName].ToString();
                    }
                    x.Add(rec);
                    ii = ii + 1;
                }
                ApiResponse<AutoCompleteResponse, SerializableDictionary<string, AutoCompleteResponse>> mr = new ApiResponse<AutoCompleteResponse, SerializableDictionary<string, AutoCompleteResponse>>();
                AutoCompleteResponse r = new AutoCompleteResponse();
                SerializableDictionary<string, string> result = new SerializableDictionary<string, string>();
                mr.errorMsg = "";
                r.data = x;
                r.query = "";
                r.topN = 999999;
                r.total = dtAuthCol.Rows.Count;
                mr.data = r;
                mr.status = "success";
                return mr;
            };
            var ret = ProtectedCall(RestrictedApiCall(fn, GetSystemId(), GetScreenId(), "R", null), AllowAnonymous());
            return ret;
        }
        [WebMethod(EnableSession = false)]
        public ApiResponse<AutoCompleteResponseObj, SerializableDictionary<string, AutoCompleteResponseObj>> GetScreenLabel()
        {
            Func<ApiResponse<AutoCompleteResponseObj, SerializableDictionary<string, AutoCompleteResponseObj>>> fn = () =>
            {
                SwitchContext(GetSystemId(), LCurr.CompanyId, LCurr.ProjectId);
                DataTable dt = _GetScreenLabel(GetScreenId());
                return DataTableToLabelResponse(dt, new List<string>() { "ColumnName", "TableId" });
            };
            var ret = ProtectedCall(RestrictedApiCall(fn, GetSystemId(), GetScreenId(), "R", null), AllowAnonymous());
            return ret;
        }
        [WebMethod(EnableSession = false)]
        public ApiResponse<SerializableDictionary<string, string>, SerializableDictionary<string, AutoCompleteResponse>> SetScreenCriteria(SerializableDictionary<string, object> criteriaValues)
        {
            Func<ApiResponse<SerializableDictionary<string, string>, SerializableDictionary<string, AutoCompleteResponse>>> fn = () =>
            {
                SwitchContext(GetSystemId(), LCurr.CompanyId, LCurr.ProjectId);
                ApiResponse<SerializableDictionary<string, string>, SerializableDictionary<string, AutoCompleteResponse>> mr = new ApiResponse<SerializableDictionary<string, string>, SerializableDictionary<string, AutoCompleteResponse>>();
                SerializableDictionary<string, string> result = new SerializableDictionary<string, string>();
                List<KeyValuePair<string, string>> validationErrs = new List<KeyValuePair<string, string>>();

                DataView dvCri = new DataView(_GetScrCriteria(GetScreenId()));
                bool isCriVisible = true;
                DataSet ds = MakeScrCriteria(GetScreenId(), dvCri, criteriaValues, true, true);

                if (validationErrs.Count == 0)
                {
                    (new AdminSystem()).UpdScrCriteria(GetScreenId().ToString(), GetProgramName(), dvCri, LUser.UsrId, isCriVisible, ds, LcAppConnString, LcAppPw);
                    mr.errorMsg = "";

                    DataTable dtLastScrCriteria = _GetLastScrCriteria(GetScreenId(), 0, true);

                    for (int ii = 1; ii < dtLastScrCriteria.Rows.Count; ii++)
                    {
                        result.Add(dvCri[ii - 1]["ColumnName"].ToString(), dtLastScrCriteria.Rows[ii]["LastCriteria"].ToString());
                    }

                    mr.data = result;
                    mr.status = "success";
                }
                else
                {
                    mr.status = "failed";
                    mr.validationErrors = validationErrs;
                    return mr;
                }

                return mr;
            };
            var ret = ProtectedCall(RestrictedApiCall(fn, GetSystemId(), GetScreenId(), "R", null));
            return ret;
        }
        [WebMethod(EnableSession = false)]
        public ApiResponse<SerializableDictionary<string, string>, SerializableDictionary<string, AutoCompleteResponse>> SetScreenFilter(string filterId)
        {
            Func<ApiResponse<SerializableDictionary<string, string>, SerializableDictionary<string, AutoCompleteResponse>>> fn = () =>
            {
                SwitchContext(GetSystemId(), LCurr.CompanyId, LCurr.ProjectId);
                ApiResponse<SerializableDictionary<string, string>, SerializableDictionary<string, AutoCompleteResponse>> mr = new ApiResponse<SerializableDictionary<string, string>, SerializableDictionary<string, AutoCompleteResponse>>();
                SerializableDictionary<string, string> result = new SerializableDictionary<string, string>();
                mr.errorMsg = "";
                mr.data = result;
                mr.status = "success";

                return mr;
            };
            var ret = ProtectedCall(RestrictedApiCall(fn, GetSystemId(), GetScreenId(), "R", null));
            return ret;
        }
        [WebMethod(EnableSession = false)]
        public ApiResponse<List<SerializableDictionary<string, string>>, SerializableDictionary<string, AutoCompleteResponse>> GetScreenTab()
        {
            Func<ApiResponse<List<SerializableDictionary<string, string>>, SerializableDictionary<string, AutoCompleteResponse>>> fn = () =>
            {
                SwitchContext(GetSystemId(), LCurr.CompanyId, LCurr.ProjectId);
                ApiResponse<List<SerializableDictionary<string, string>>, SerializableDictionary<string, AutoCompleteResponse>> mr = new ApiResponse<List<SerializableDictionary<string, string>>, SerializableDictionary<string, AutoCompleteResponse>>();
                SerializableDictionary<string, string> result = new SerializableDictionary<string, string>();
                DataTable dt = _GetScreenTab(GetScreenId());
                mr.errorMsg = "";
                mr.data = DataTableToListOfObject(dt); ;
                mr.status = "success";

                return mr;
            };
            var ret = ProtectedCall(RestrictedApiCall(fn, GetSystemId(), GetScreenId(), "R", null), AllowAnonymous());
            return ret;
        }
        [WebMethod(EnableSession = false)]
        public ApiResponse<SerializableDictionary<string, string>, SerializableDictionary<string, AutoCompleteResponse>> GetScreenHlp()
        {
            Func<ApiResponse<SerializableDictionary<string, string>, SerializableDictionary<string, AutoCompleteResponse>>> fn = () =>
            {
                SwitchContext(GetSystemId(), LCurr.CompanyId, LCurr.ProjectId);
                ApiResponse<SerializableDictionary<string, string>, SerializableDictionary<string, AutoCompleteResponse>> mr = new ApiResponse<SerializableDictionary<string, string>, SerializableDictionary<string, AutoCompleteResponse>>();
                SerializableDictionary<string, string> result = new SerializableDictionary<string, string>();
                DataTable dt = _GetScreenHlp(GetScreenId());
                mr.errorMsg = "";
                mr.data = DataTableToListOfObject(dt)[0]; ;
                mr.status = "success";

                return mr;
            };
            var ret = ProtectedCall(RestrictedApiCall(fn, GetSystemId(), GetScreenId(), "R", null), AllowAnonymous());
            return ret;
        }
        [WebMethod(EnableSession = false)]
        public ApiResponse<List<SerializableDictionary<string, string>>, SerializableDictionary<string, AutoCompleteResponse>> GetMenu(byte systemId)
        {
            Func<ApiResponse<List<SerializableDictionary<string, string>>, SerializableDictionary<string, AutoCompleteResponse>>> fn = () =>
            {
                SwitchContext(systemId, LCurr.CompanyId, LCurr.ProjectId);
             
                SerializableDictionary<string, string> result = new SerializableDictionary<string, string>();
               DataTable dtMenu = _GetMenu(LCurr.SystemId, true);
               
                ApiResponse<List<SerializableDictionary<string, string>>, SerializableDictionary<string, AutoCompleteResponse>> mr = new ApiResponse<List<SerializableDictionary<string, string>>, SerializableDictionary<string, AutoCompleteResponse>>();
                mr.data = DataTableToListOfObject(dtMenu, false, null);
                mr.status = "success";
                mr.errorMsg = "";
                return mr;
            };
            var ret = ProtectedCall(RestrictedApiCall(fn, GetSystemId(), 0, "R", null));
            return ret;
        }

        [WebMethod(EnableSession = false)]
        public ApiResponse<List<SerializableDictionary<string, string>>, SerializableDictionary<string, AutoCompleteResponse>> GetReactQuickMenu(byte systemId)
        {
            Func<ApiResponse<List<SerializableDictionary<string, string>>, SerializableDictionary<string, AutoCompleteResponse>>> fn = () =>
            {
                SwitchContext(systemId, LCurr.CompanyId, LCurr.ProjectId);

                SerializableDictionary<string, string> result = new SerializableDictionary<string, string>();
                ApiResponse<List<SerializableDictionary<string, string>>, SerializableDictionary<string, AutoCompleteResponse>> mr = new ApiResponse<List<SerializableDictionary<string, string>>, SerializableDictionary<string, AutoCompleteResponse>>();
                DataTable dtMenu = _GetMenu(LCurr.SystemId, true);
                if (dtMenu.Rows.Count > 0)
                {
                    var reactMenu = dtMenu.AsEnumerable().Where(dr => dr["ReactQuickMenu"].ToString() == "Y");
                    DataTable dtMenuFiltered = reactMenu.Any() ? reactMenu.CopyToDataTable() : dtMenu.Clone();
                    mr.data = DataTableToListOfObject(dtMenuFiltered, IncludeBLOB.None, null, null, new List<string>() { "MenuText", "NavigateUrl", "ReactQuickMenu" });
                    mr.status = "success";
                    mr.errorMsg = "";
                    return mr;
                }
                else
                {
                    mr.status = "failed";
                    mr.errorMsg = "No quick menu available";
                    mr.data = null;
                    return mr;
                }
            };
            var ret = ProtectedCall(RestrictedApiCall(fn, GetSystemId(), 0, "R", null));
            return ret;
        }


        [WebMethod(EnableSession = false)]
        public ApiResponse<List<SerializableDictionary<string, string>>, SerializableDictionary<string, AutoCompleteResponse>> GetSystems(bool ignoreCache = false)
        {
            Func<ApiResponse<List<SerializableDictionary<string, string>>, SerializableDictionary<string, AutoCompleteResponse>>> fn = () =>
            {
                //SwitchContext(systemId, LCurr.CompanyId, LCurr.ProjectId);
                SerializableDictionary<string, string> result = new SerializableDictionary<string, string>();
                DataTable dt = LoadSystemsList(true);

                ApiResponse<List<SerializableDictionary<string, string>>, SerializableDictionary<string, AutoCompleteResponse>> mr = new ApiResponse<List<SerializableDictionary<string, string>>, SerializableDictionary<string, AutoCompleteResponse>>();
                mr.data = DataTableToListOfObject(dt, IncludeBLOB.None, null, null, new List<string>() { "SystemId", "SystemAbbr", "SystemName", "Active" });
                mr.status = "success";
                mr.errorMsg = "";
                return mr;
            };
            var ret = ProtectedCall(RestrictedApiCall(fn, GetSystemId(), 0, "R", null));
            return ret;
        }

        [WebMethod(EnableSession = false)]
        public ApiResponse<SerializableDictionary<string, string>, object> GetServerIdentity()
        {
            Func<ApiResponse<SerializableDictionary<string, string>, object>> fn = () =>
            {
                string ServerIdentity = System.Configuration.ConfigurationManager.AppSettings["ServerIdentity"];

                ApiResponse<SerializableDictionary<string, string>, object> mr = new ApiResponse<SerializableDictionary<string, string>, object>();

                SerializableDictionary<string, string> result = new SerializableDictionary<string, string>();

                result.Add("serverIdentity", ServerIdentity);

                mr.status = "success";
                mr.errorMsg = "";
                mr.data = result;

                return mr;
            };
            var ret = ProtectedCall(RestrictedApiCall(fn, GetSystemId(), 0, "R", null));
            return ret;
        }

        [WebMethod(EnableSession = false)]
        public ApiResponse<AutoCompleteResponse, SerializableDictionary<string, AutoCompleteResponse>> Labels(string labelCat)
        {
            return GetLabels(labelCat);
        }
        [WebMethod(EnableSession = false)]
        public ApiResponse<AutoCompleteResponse, SerializableDictionary<string, AutoCompleteResponse>> GetLabels(string labelCat)
        {
            Func<ApiResponse<AutoCompleteResponse, SerializableDictionary<string, AutoCompleteResponse>>> fn = () =>
            {
                SwitchContext(GetSystemId(), LCurr.CompanyId, LCurr.ProjectId);
                DataTable dt = _GetLabels(labelCat);
                return DataTableToApiResponse(dt, "", 0);
            };
            var ret = ProtectedCall(RestrictedApiCall(fn, GetSystemId(), 0, "R", null));
            return ret;
        }
        [WebMethod(EnableSession = false)]
        public ApiResponse<AutoCompleteResponse, SerializableDictionary<string, AutoCompleteResponse>> GetSystemLabels(string labelCat)
        {
            Func<ApiResponse<AutoCompleteResponse, SerializableDictionary<string, AutoCompleteResponse>>> fn = () =>
            {
                SwitchContext(3, LCurr.CompanyId, LCurr.ProjectId);
                DataTable dtS = _GetLabels("cSystem");
                dtS.Merge(_GetLabels("QFilter"));
                dtS.Merge(_GetLabels("cGrid"));
                return DataTableToApiResponse(dtS, "", 0);
            };
            var ret = ProtectedCall(RestrictedApiCall(fn, 3, 0, "R", null), AllowAnonymous());
            return ret;
        }
        [WebMethod(EnableSession = false)]
        public ApiResponse<AutoCompleteResponse, SerializableDictionary<string, AutoCompleteResponse>> GetScreenButtonHlp()
        {
            Func<ApiResponse<AutoCompleteResponse, SerializableDictionary<string, AutoCompleteResponse>>> fn = () =>
            {
                SwitchContext(GetSystemId(), LCurr.CompanyId, LCurr.ProjectId);
                DataTable dt = _GetScreenButtonHlp(GetScreenId());
                return DataTableToApiResponse(dt, "", 0);
            };
            var ret = ProtectedCall(RestrictedApiCall(fn, GetSystemId(), GetScreenId(), "R", null), AllowAnonymous());
            return ret;
        }
        [WebMethod(EnableSession = false)]
        public ApiResponse<AutoCompleteResponseObj, SerializableDictionary<string, AutoCompleteResponseObj>> GetScreenCriteriaEX(bool refresh)
        {
            Func<ApiResponse<AutoCompleteResponseObj, SerializableDictionary<string, AutoCompleteResponseObj>>> fn = () =>
            {
                SwitchContext(GetSystemId(), LCurr.CompanyId, LCurr.ProjectId);
                DataTable dt = _GetScrCriteria(GetScreenId());
                if (!dt.Columns.Contains("LastCriteria"))
                {
                    dt.Columns.Add("LastCriteria", typeof(string));
                }
                DataTable dtLastScrCriteria = _GetLastScrCriteria(GetScreenId(), 0, refresh);
                //skip first row in last criteria
                for (int ii = 1; ii < dtLastScrCriteria.Rows.Count && (ii-1) < dt.Rows.Count; ii++)
                {
                    dt.Rows[ii - 1]["LastCriteria"] = dtLastScrCriteria.Rows[ii]["LastCriteria"];
                }
                return DataTableToLabelResponse(dt, new List<string>() { "ColumnName" });
            };
            var ret = ProtectedCall(RestrictedApiCall(fn, GetSystemId(), GetScreenId(), "R", null), AllowAnonymous());
            return ret;
        }

        [WebMethod(EnableSession = false)]
        public ApiResponse<AutoCompleteResponseObj, SerializableDictionary<string, AutoCompleteResponseObj>> GetScreenCriteria()
        {

            return GetScreenCriteriaEX(false);
        }

        [WebMethod(EnableSession = false)]
        public ApiResponse<AutoCompleteResponse, SerializableDictionary<string, AutoCompleteResponse>> GetScreenCriteriaDef()
        {
            Func<ApiResponse<AutoCompleteResponse, SerializableDictionary<string, AutoCompleteResponse>>> fn = () =>
            {
                SwitchContext(GetSystemId(), LCurr.CompanyId, LCurr.ProjectId);
                DataTable dt = _GetScrCriteria(GetScreenId());
                return DataTableToApiResponse(dt, "", 0);
            };
            var ret = ProtectedCall(RestrictedApiCall(fn, GetSystemId(), GetScreenId(), "R", null), AllowAnonymous());
            return ret;
        }
        [WebMethod(EnableSession = false)]
        public ApiResponse<AutoCompleteResponse, SerializableDictionary<string, AutoCompleteResponse>> GetScreenCriteriaDdlList(string screenCriId, string query, int topN, string filterBy)
        {
            Func<ApiResponse<AutoCompleteResponse, SerializableDictionary<string, AutoCompleteResponse>>> fn = () =>
            {
                SwitchContext(GetSystemId(), LCurr.CompanyId, LCurr.ProjectId);
                DataTable dt = _GetScrCriteria(GetScreenId());
                var drv = (from r in dt.AsEnumerable() where r.Field<int?>("ScreenCriId").ToString() == screenCriId select r).FirstOrDefault();
                if (drv == null)
                {
                    throw new Exception(string.Format("Criteria Id {0} not found", screenCriId));
                }
                else if (drv["DisplayMode"].ToString() == "AutoComplete")
                {
                    System.Collections.Generic.Dictionary<string, string> context = new System.Collections.Generic.Dictionary<string, string>();
                    context["method"] = "GetScreenIn";
                    context["addnew"] = "Y";
                    context["sp"] = "GetDdl" + drv["ColumnName"].ToString() + GetSystemId().ToString() + "C" + screenCriId;
                    context["requiredValid"] = drv["RequiredValid"].ToString();
                    context["mKey"] = drv["DdlKeyColumnName"].ToString();
                    context["mVal"] = drv["DdlRefColumnName"].ToString();
                    context["mTip"] = drv["DdlRefColumnName"].ToString();
                    context["mImg"] = drv["DdlRefColumnName"].ToString();
                    context["ssd"] = "";
                    context["scr"] = GetScreenId().ToString();
                    context["csy"] = GetSystemId().ToString();
                    context["filter"] = "0";
                    context["isSys"] = "N";
                    context["conn"] = LcSysConnString;
                    context["refColCID"] = "";
                    context["refCol"] = drv["DdlFtrColumnName"].ToString();
                    context["refColDataType"] = drv["DdlFtrDataType"].ToString();
                    context["refColVal"] = filterBy;
                    ApiResponse<AutoCompleteResponse, SerializableDictionary<string, AutoCompleteResponse>> mr = new ApiResponse<AutoCompleteResponse, SerializableDictionary<string, AutoCompleteResponse>>();
                    mr.status = "success";
                    mr.errorMsg = "";
                    mr.data = ddlSuggests(query, context, topN);
                    return mr;
                }
                else
                {
                    try
                    {
                        _MkScreenIn(GetScreenId(), screenCriId, "GetDdl" + drv["ColumnName"].ToString() + GetSystemId().ToString() + "C" + screenCriId, drv["MultiDesignDb"].ToString(), false);
                        return DataTableToApiResponse((new AdminSystem()).GetScreenIn(GetScreenId().ToString(), "GetDdl" + drv["ColumnName"].ToString() + GetSystemId().ToString() + "C" + drv["ScreenCriId"].ToString(), (new AdminSystem()).CountScrCri(drv["ScreenCriId"].ToString(), drv["MultiDesignDb"].ToString(), LcSysConnString, LcAppPw), drv["RequiredValid"].ToString(), 0, string.Empty, LImpr, LCurr, LcAppConnString, LcAppPw), "", 0);
                    }
                    catch (Exception ex)
                    {
                        ErrorTracing(new Exception(string.Format("{0} {1}", LcAppConnString, GetSystemId()), ex));
                        throw;
                    }
                }
            };
            var ret = ProtectedCall(RestrictedApiCall(fn, GetSystemId(), GetScreenId(), "R", null), AllowAnonymous());
            return ret;
        }

        [WebMethod(EnableSession = false)]
        public ApiResponse<AutoCompleteResponse, SerializableDictionary<string, AutoCompleteResponse>> GetScreenFilter()
        {
            Func<ApiResponse<AutoCompleteResponse, SerializableDictionary<string, AutoCompleteResponse>>> fn = () =>
            {
                SwitchContext(GetSystemId(), LCurr.CompanyId, LCurr.ProjectId);
                DataTable dt = _GetScreenFilter(GetScreenId());
                return DataTableToApiResponse(dt, "", 0);
            };
            var ret = ProtectedCall(RestrictedApiCall(fn, GetSystemId(), GetScreenId(), "R", null), AllowAnonymous());
            return ret;
        }

        [WebMethod(EnableSession = false)]
        public ApiResponse<List<SerializableDictionary<string, string>>, SerializableDictionary<string, AutoCompleteResponse>> GetColumnContent(string mstId, string dtlId, string columnName, bool isMaster, string screenColumnName)
        {

            Func<ApiResponse<List<SerializableDictionary<string, string>>, SerializableDictionary<string, AutoCompleteResponse>>> fn = () =>
            {
                SwitchContext(GetSystemId(), LCurr.CompanyId, LCurr.ProjectId);
                ValidatedMstId(GetValidateMstIdSPName(), GetSystemId(), GetScreenId(), "**" + mstId, MatchScreenCriteria(new DataView(_GetScrCriteria(GetScreenId())), null));
                DataTable dtAut = _GetAuthCol(GetScreenId());
                DataTable dtLabel = _GetScreenLabel(GetScreenId());

                string keyColumName = isMaster ? GetMstKeyColumnName(true) : GetDtlKeyColumnName(true);
                string tableName = isMaster ? GetMstTableName(true) : GetDtlTableName(true);
                int ii = 0;
                DataTable dt = null;
                ApiResponse<List<SerializableDictionary<string, string>>, SerializableDictionary<string, AutoCompleteResponse>> mr = new ApiResponse<List<SerializableDictionary<string, string>>, SerializableDictionary<string, AutoCompleteResponse>>();
                if (string.IsNullOrEmpty(mstId) || (string.IsNullOrEmpty(dtlId) && !isMaster))
                {
                    mr.data = null;
                    mr.status = "failed";
                    mr.errorMsg = "invalid key";
                    return mr;
                }

                foreach (DataRow drLabel in dtLabel.Rows)
                {
                    DataRow drAuth = dtAut.Rows[ii];
                    if ("HyperLink,ImageButton".IndexOf(drLabel["DisplayName"].ToString()) >= 0
                        && !string.IsNullOrEmpty(drLabel["TableId"].ToString())
                        && drAuth["MasterTable"].ToString() == (isMaster ? "Y" : "N")
                        && drAuth["ColVisible"].ToString() == "Y"
                        && (drLabel["ColumnName"].ToString() + drLabel["TableId"].ToString()) == screenColumnName)
                    {
                        /* both are technically wrong as there is no info for the underlying tablecolumnname in GetScreenLabel or GetAuthCol 
                         * should add that info in one of them. we are just assuming the same imagebutton field would not appear more than once in a screen
                         * and use drLabel's version
                         */
                        //Dictionary<string, DataRow> colAuth = dtAut.AsEnumerable().ToDictionary(dr => dr["ColName"].ToString());
                        //var utcColumnList = dtLabel.AsEnumerable().Where(dr => dr["DisplayMode"].ToString().Contains("UTC")).Select(dr => dr["ColumnName"].ToString() + dr["TableId"].ToString()).ToArray();
                        //HashSet<string> utcColumns = new HashSet<string>(utcColumnList);
                        string colName = drAuth["ColName"].ToString();
                        string tableColumnName = drLabel["ColumnName"].ToString();
                        dt = (new AdminSystem()).GetDbImg(isMaster ? mstId : dtlId, tableName, keyColumName, tableColumnName, LcAppConnString, LcAppPw);
                        try
                        {
                            // can't pass in the colauth or it would be filtered out DB column name vs Screen column name, FIXEM
                            mr.data = DataTableToListOfObject(dt, IncludeBLOB.Content, null, null);
                            mr.status = "success";
                            mr.errorMsg = "";
                            return mr;
                        }
                        catch (Exception ex)
                        {
                            throw new Exception(string.Format("GetColumnContent Error for {0}", screenColumnName), ex);
                        }
                    }
                    ii = ii + 1;
                }
                mr.data = null;
                mr.status = "unauthorized_access";
                mr.errorMsg = "access denied";
                return mr;
            };
            var ret = ProtectedCall(RestrictedApiCall(fn, GetSystemId(), GetScreenId(), "R", screenColumnName, emptyListResponse), AllowAnonymous());
            return ret;
        }

        [WebMethod(EnableSession = false)]
        public ApiResponse<List<SerializableDictionary<string, string>>, SerializableDictionary<string, AutoCompleteResponse>> GetRefColumnContent(string mstId, string dtlId, string refKeyId, bool isMaster, string refScreenColumnName, SerializableDictionary<string, string> options)
        {

            Func<ApiResponse<List<SerializableDictionary<string, string>>, SerializableDictionary<string, AutoCompleteResponse>>> fn = () =>
            {
                SwitchContext(GetSystemId(), LCurr.CompanyId, LCurr.ProjectId);
                //ValidatedMstId(GetValidateMstIdSPName(), GetSystemId(), GetScreenId(), "**" + mstId, MatchScreenCriteria(new DataView(_GetScrCriteria(GetScreenId())), null));
                string blobIconOption = !options.ContainsKey("Blob") ? "I" : options["Blob"];
                var blob = GetBlobOption(blobIconOption);
                DataTable dtAut = _GetAuthCol(GetScreenId());
                DataTable dtLabel = _GetScreenLabel(GetScreenId());

                string keyColumName = isMaster ? GetMstKeyColumnName(true) : GetDtlKeyColumnName(true);
                string tableName = isMaster ? GetMstTableName(true) : GetDtlTableName(true);
                int ii = 0;
                DataTable dt = null;
                ApiResponse<List<SerializableDictionary<string, string>>, SerializableDictionary<string, AutoCompleteResponse>> mr = new ApiResponse<List<SerializableDictionary<string, string>>, SerializableDictionary<string, AutoCompleteResponse>>();
                //if (string.IsNullOrEmpty(mstId) || (string.IsNullOrEmpty(dtlId) && !isMaster))
                //{
                //    mr.data = null;
                //    mr.status = "failed";
                //    mr.errorMsg = "invalid key";
                //    return mr;
                //}

                foreach (DataRow drLabel in dtLabel.Rows)
                {
                    DataRow drAuth = dtAut.Rows[ii];
                    if (
                        !string.IsNullOrEmpty(drLabel["TableId"].ToString())
                        && drAuth["MasterTable"].ToString() == (isMaster ? "Y" : "N")
                        && drAuth["ColVisible"].ToString() == "Y"
                        && (drLabel["ColumnName"].ToString() + drLabel["TableId"].ToString()) == refScreenColumnName)
                    {
                        /* both are technically wrong as there is no info for the underlying tablecolumnname in GetScreenLabel or GetAuthCol 
                         * should add that info in one of them. we are just assuming the same imagebutton field would not appear more than once in a screen
                         * and use drLabel's version
                         */
                        string colName = drAuth["ColName"].ToString();
                        string tableColumnName = drLabel["ColumnName"].ToString();
                        var ddlContext = GetDdlContext();
                        string ddlSP = ddlContext[refScreenColumnName]["method"];
                        Dictionary<string, Dictionary<string, string>> ddlDependents = ddlContext
                                                                            .GroupBy((e) => e.Value["method"])
                                                                            .Select((g) =>
                                                                            {
                                                                                return new {
                                                                                Key = g.Key,
                                                                                Value = g.Select(v=>new Tuple<string,string>(v.Key, v.Value["mVal"]))
                                                                                        .ToDictionary(v=>v.Item1, v=>v.Item2)
                                                                                };
                                                                            }
                                                                            ).ToDictionary(v=>v.Key, v=>v.Value);

                        Dictionary<string, DataRow> colAuth = dtAut.AsEnumerable()
                                                                .ToDictionary(dr => {
                                                                    var x = ddlDependents[ddlSP];
                                                                    var screenColumName = dr["ColName"].ToString();
                                                                    return x.ContainsKey(screenColumName) ? x[screenColumName] : screenColumName;
                                                                });
                        var utcColumnList = dtLabel
                                            .AsEnumerable()
                                            .Where(dr => dr["DisplayMode"].ToString().Contains("UTC"))
                                            .Select(dr => {
                                                var x = ddlDependents[ddlSP];
                                                var screenColumName = dr["ColumnName"].ToString() + dr["TableId"].ToString();
                                                return x.ContainsKey(screenColumName) ? x[screenColumName] : screenColumName;
                                            }
                                                )
                                            .ToArray();
                        HashSet<string> utcColumns = new HashSet<string>(utcColumnList);
                        dt = GetDdl(ddlSP, false, GetSystemId(), GetScreenId(), "**" + refKeyId, "", "N", "", "N", 1);
                        try
                        {
                            // can't pass in the colauth or it would be filtered out DB column name vs Screen column name, FIXEM
                            mr.data = DataTableToListOfObject(dt, blob, colAuth, utcColumns);
                            mr.status = "success";
                            mr.errorMsg = "";
                            return mr;
                        }
                        catch (Exception ex)
                        {
                            throw new Exception(string.Format("GetRefColumnContent Error for {0}", refScreenColumnName), ex);
                        }
                    }
                    ii = ii + 1;
                }
                mr.data = null;
                mr.status = "unauthorized_access";
                mr.errorMsg = "access denied";
                return mr;
            };
            var ret = ProtectedCall(RestrictedApiCall(fn, GetSystemId(), GetScreenId(), "R", refScreenColumnName, emptyListResponse), AllowAnonymous());
            return ret;
        }

        [WebMethod(EnableSession = false)]
        public ApiResponse<SaveDataResponse, SerializableDictionary<string, AutoCompleteResponse>> AddDocColumnContent(string mstId, string dtlId, bool isMaster, string screenColumnName, string docJson, SerializableDictionary<string, string> options)
        {

            Func<ApiResponse<SaveDataResponse, SerializableDictionary<string, AutoCompleteResponse>>> fn = () =>
            {
                SwitchContext(GetSystemId(), LCurr.CompanyId, LCurr.ProjectId);
                ValidatedMstId(GetValidateMstIdSPName(), GetSystemId(), GetScreenId(), "**" + mstId, MatchScreenCriteria(new DataView(_GetScrCriteria(GetScreenId())), null));
                DataTable dtAut = _GetAuthCol(GetScreenId());
                DataTable dtLabel = _GetScreenLabel(GetScreenId());
                List<KeyValuePair<string, string>> errors = new List<KeyValuePair<string, string>>();
                int ii = 0;
                foreach (DataRow drLabel in dtLabel.Rows)
                {
                    DataRow drAuth = dtAut.Rows[ii];
                    if ("HyperLink,ImageButton".IndexOf(drLabel["DisplayName"].ToString()) >= 0
                        && !string.IsNullOrEmpty(drLabel["TableId"].ToString())
                        && drAuth["MasterTable"].ToString() == (isMaster ? "Y" : "N")
                        && drAuth["ColVisible"].ToString() == "Y"
                        && (drLabel["ColumnName"].ToString() + drLabel["TableId"].ToString()) == screenColumnName)
                    {
                        /* both are technically wrong as there is no info for the underlying tablecolumnname in GetScreenLabel or GetAuthCol 
                         * should add that info in one of them. we are just assuming the same imagebutton field would not appear more than once in a screen
                         * and use drLabel's version
                         */
                        string colName = drAuth["ColName"].ToString();
                        string tableColumnName = drLabel["ColumnName"].ToString();
                        SerializableDictionary<string, string> currentMst = new SerializableDictionary<string, string>();
                        SerializableDictionary<string, string> revisedMst = new SerializableDictionary<string, string>() { { screenColumnName, docJson } };
                        ValidateField(drAuth, drLabel, currentMst, InitMaster(), ref revisedMst, null, ref errors, null);
                        if (errors.Count == 0)
                        {
                            bool canUpdateEmbeddedDoc = PreSaveEmbeddedDoc(docJson,
                                isMaster ? mstId : dtlId,
                                isMaster ? GetMstTableName(true) : GetDtlTableName(true),
                                isMaster ? GetMstKeyColumnName(true) : GetDtlKeyColumnName(true),
                                tableColumnName);
                            List<_ReactFileUploadObj> savedObj = AddDoc(docJson, isMaster ? mstId : dtlId, isMaster ? GetMstTableName(true) : GetDtlTableName(true), isMaster ? GetMstKeyColumnName(true) : GetDtlKeyColumnName(true), tableColumnName, options.ContainsKey("resizeImage"));
                            PostSaveEmbeddedDoc(isMaster ? mstId : dtlId, isMaster ? GetMstTableName(true) : GetDtlTableName(true), isMaster ? GetMstKeyColumnName(true) : GetDtlKeyColumnName(true), tableColumnName);

                            System.Web.Script.Serialization.JavaScriptSerializer jss = new System.Web.Script.Serialization.JavaScriptSerializer();
                            jss.MaxJsonLength = Int32.MaxValue;
                            ApiResponse<SaveDataResponse, SerializableDictionary<string, AutoCompleteResponse>> mr = new ApiResponse<SaveDataResponse, SerializableDictionary<string, AutoCompleteResponse>>();
                            SaveDataResponse result = new SaveDataResponse()
                            {
                                mst = isMaster && savedObj != null ? new SerializableDictionary<string, string>() { { "fileObject", jss.Serialize(savedObj) } } : null
                                ,
                                dtl = !isMaster && savedObj != null ? new List<SerializableDictionary<string, string>>() {
                                    new SerializableDictionary<string, string>() {{"fileObject", jss.Serialize(savedObj)}}
                                } : null
                            };

                            string msg = "image updated";
                            result.message = msg;
                            mr.status = "success";
                            mr.errorMsg = "";
                            mr.data = result;
                            return mr;
                        }
                        break;
                    }
                    ii = ii + 1;
                }

                return new ApiResponse<SaveDataResponse, SerializableDictionary<string, AutoCompleteResponse>>()
                {
                    status = "failed",
                    errorMsg = "content invalid " + string.Join(" ", errors).ToArray(),
                    validationErrors = errors,
                };


            };
            var ret = ProtectedCall(RestrictedApiCall(fn, GetSystemId(), GetScreenId(), "S", screenColumnName));
            return ret;
        }

        [WebMethod(EnableSession = false)]
        public ApiResponse<SaveDataResponse, SerializableDictionary<string, AutoCompleteResponse>> DecryptedColumnContent(string mstId, string dtlId, bool isMaster, string screenColumnName, string content, string key)
        {

            Func<ApiResponse<SaveDataResponse, SerializableDictionary<string, AutoCompleteResponse>>> fn = () =>
            {
                SwitchContext(GetSystemId(), LCurr.CompanyId, LCurr.ProjectId);
                ValidatedMstId(GetValidateMstIdSPName(), GetSystemId(), GetScreenId(), "**" + mstId, MatchScreenCriteria(new DataView(_GetScrCriteria(GetScreenId())), null));
                DataTable dtAut = _GetAuthCol(GetScreenId());
                DataTable dtLabel = _GetScreenLabel(GetScreenId());
                List<KeyValuePair<string, string>> errors = new List<KeyValuePair<string, string>>();
                int ii = 0;
                foreach (DataRow drLabel in dtLabel.Rows)
                {
                    DataRow drAuth = dtAut.Rows[ii];
                    if ("EncryptedTextBox".IndexOf(drLabel["DisplayMode"].ToString()) >= 0
                        && !string.IsNullOrEmpty(drLabel["TableId"].ToString())
                        && drAuth["MasterTable"].ToString() == (isMaster ? "Y" : "N")
                        && drAuth["ColVisible"].ToString() == "Y"
                        && (drLabel["ColumnName"].ToString() + drLabel["TableId"].ToString()) == screenColumnName)
                    {
                        ApiResponse<SaveDataResponse, SerializableDictionary<string, AutoCompleteResponse>> mr = new ApiResponse<SaveDataResponse, SerializableDictionary<string, AutoCompleteResponse>>();
                        string text = content;
                        string actualValue = content;
                        try
                        {
                            int visiblePart = text.IndexOf('-');
                            string encryptedValue = visiblePart > 0 ? text.Substring(0, visiblePart) : text;
                            actualValue = RO.Common3.Utils.RODecryptString(encryptedValue, string.IsNullOrEmpty(key) ? Config.SecuredColumnKey : key);
                        }
                        catch
                        {
                        }

                        SaveDataResponse result = new SaveDataResponse()
                        {
                            mst = isMaster ? new SerializableDictionary<string, string>() { { "actualValue", actualValue } } : null
                            ,
                            dtl = !isMaster ? new List<SerializableDictionary<string, string>>() {
                                    new SerializableDictionary<string, string>() {{"actualValue", actualValue }}
                                } : null
                        };

                        string msg = "";
                        result.message = msg;
                        mr.status = "success";
                        mr.errorMsg = "";
                        mr.data = result;
                        return mr;
                    }
                    ii = ii + 1;
                }

                return new ApiResponse<SaveDataResponse, SerializableDictionary<string, AutoCompleteResponse>>()
                {
                    status = "unauthorized_access",
                    errorMsg = "access_denied",
                };


            };
            var ret = ProtectedCall(RestrictedApiCall(fn, GetSystemId(), GetScreenId(), "R", screenColumnName));
            return ret;
        }
        [WebMethod(EnableSession = false)]
        public ApiResponse<SaveDataResponse, SerializableDictionary<string, AutoCompleteResponse>> EncryptedColumnContent(string mstId, string dtlId, bool isMaster, string screenColumnName, string content, string key)
        {

            Func<ApiResponse<SaveDataResponse, SerializableDictionary<string, AutoCompleteResponse>>> fn = () =>
            {
                SwitchContext(GetSystemId(), LCurr.CompanyId, LCurr.ProjectId);
                ValidatedMstId(GetValidateMstIdSPName(), GetSystemId(), GetScreenId(), "**" + mstId, MatchScreenCriteria(new DataView(_GetScrCriteria(GetScreenId())), null));
                DataTable dtAut = _GetAuthCol(GetScreenId());
                DataTable dtLabel = _GetScreenLabel(GetScreenId());
                List<KeyValuePair<string, string>> errors = new List<KeyValuePair<string, string>>();
                int ii = 0;
                foreach (DataRow drLabel in dtLabel.Rows)
                {
                    DataRow drAuth = dtAut.Rows[ii];
                    if ("EncryptedTextBox".IndexOf(drLabel["DisplayMode"].ToString()) >= 0
                        && !string.IsNullOrEmpty(drLabel["TableId"].ToString())
                        && drAuth["MasterTable"].ToString() == (isMaster ? "Y" : "N")
                        && drAuth["ColVisible"].ToString() == "Y"
                        && (drLabel["ColumnName"].ToString() + drLabel["TableId"].ToString()) == screenColumnName)
                    {
                        ApiResponse<SaveDataResponse, SerializableDictionary<string, AutoCompleteResponse>> mr = new ApiResponse<SaveDataResponse, SerializableDictionary<string, AutoCompleteResponse>>();
                        string encryptedValue = content;
                        try
                        {
                            encryptedValue = RO.Common3.Utils.ROEncryptString(content, string.IsNullOrEmpty(key) ? Config.SecuredColumnKey : key);
                        }
                        catch
                        {
                        }

                        SaveDataResponse result = new SaveDataResponse()
                        {
                            mst = isMaster ? new SerializableDictionary<string, string>() { { "encryptedValue", encryptedValue } } : null
                            ,
                            dtl = !isMaster ? new List<SerializableDictionary<string, string>>() {
                                    new SerializableDictionary<string, string>() {{"encryptedValue", encryptedValue }}
                                } : null
                        };

                        string msg = "";
                        result.message = msg;
                        mr.status = "success";
                        mr.errorMsg = "";
                        mr.data = result;
                        return mr;
                    }
                    ii = ii + 1;
                }

                return new ApiResponse<SaveDataResponse, SerializableDictionary<string, AutoCompleteResponse>>()
                {
                    status = "unauthorized_access",
                    errorMsg = "access_denied",
                };
            };
            var ret = ProtectedCall(RestrictedApiCall(fn, GetSystemId(), GetScreenId(), "R", screenColumnName));
            return ret;
        }

        [WebMethod(EnableSession = false)]
        public ApiResponse<List<SerializableDictionary<string, string>>, SerializableDictionary<string, AutoCompleteResponse>> GetDoc(string mstId, string dtlId, bool isMaster, string docId, string screenColumnName)
        {
            Func<ApiResponse<List<SerializableDictionary<string, string>>, SerializableDictionary<string, AutoCompleteResponse>>> fn = () =>
            {
                SwitchContext(GetSystemId(), LCurr.CompanyId, LCurr.ProjectId);
                ValidatedMstId(GetValidateMstIdSPName(), GetSystemId(), GetScreenId(), "**" + mstId, MatchScreenCriteria(new DataView(_GetScrCriteria(GetScreenId())), null));
                string tableColumnName = ValidatedColAuth(screenColumnName, isMaster, "Document", null, false);
                DataTable dtDocList = _GetDocList(isMaster ? mstId : dtlId, screenColumnName);
                bool hasDoc = (from x in dtDocList.AsEnumerable() where !string.IsNullOrEmpty(docId) && x["DocId"].ToString() == docId select x).Count() > 0;
                ApiResponse<List<SerializableDictionary<string, string>>, SerializableDictionary<string, AutoCompleteResponse>> mr = new ApiResponse<List<SerializableDictionary<string, string>>, SerializableDictionary<string, AutoCompleteResponse>>();

                if (hasDoc && !string.IsNullOrEmpty(tableColumnName))
                {
                    DataTable dt = (new AdminSystem()).GetDbDoc(docId, _GetDocTableName(screenColumnName), LcAppConnString, LcAppPw);
                    mr.data = DataTableToListOfObject(dt, IncludeBLOB.Content);
                    mr.status = "success";
                    mr.errorMsg = "";
                    return mr;
                }
                else
                {
                    return new ApiResponse<List<SerializableDictionary<string, string>>, SerializableDictionary<string, AutoCompleteResponse>>()
                    {
                        data = null,
                        status = "unauthorized_access",
                        errorMsg = "access denied"
                    };
                }
            };
            var ret = ProtectedCall(RestrictedApiCall(fn, GetSystemId(), GetScreenId(), "R", screenColumnName, emptyListResponse), AllowAnonymous());
            return ret;
        }

        [WebMethod(EnableSession = false)]
        public ApiResponse<AutoCompleteResponse, SerializableDictionary<string, AutoCompleteResponse>> GetDocList(string mstId, string dtlId, bool isMaster, string screenColumnName)
        {
            Func<ApiResponse<AutoCompleteResponse, SerializableDictionary<string, AutoCompleteResponse>>> fn = () =>
            {
                SwitchContext(GetSystemId(), LCurr.CompanyId, LCurr.ProjectId);
                ValidatedMstId(GetValidateMstIdSPName(), GetSystemId(), GetScreenId(), "**" + mstId, MatchScreenCriteria(new DataView(_GetScrCriteria(GetScreenId())), null));
                string tableColumnName = ValidatedColAuth(screenColumnName, isMaster, "Document", null, false);
                DataTable dt = _GetDocList(isMaster ? mstId : dtlId, screenColumnName);
                for (int i = dt.Rows.Count; i < dt.Rows.Count; i++)
                {
                    DataRow dr = dt.Rows[i];
                    if (string.IsNullOrEmpty(tableColumnName))
                        dt.Rows.Remove(dr);
                    else
                    {
                        string url = ResolveUrlCustom(dr["DocLink"].ToString(), false, true);
                        string securedUrl = GetUrlWithQSHashV2(url);
                        dr["DocLink"] = securedUrl;
                    }
                }
                dt.AcceptChanges();
                return DataTableToApiResponse(dt, "", 0);
            };
            var ret = ProtectedCall(RestrictedApiCall(fn, GetSystemId(), GetScreenId(), "R", screenColumnName, emptyAutoCompleteResponse), AllowAnonymous());
            return ret;
        }

        [WebMethod(EnableSession = false)]
        public ApiResponse<LoadScreenPageResponse, SerializableDictionary<string, AutoCompleteResponse>> GetScreenMetaData(SerializableDictionary<string, string> options)
        {
            int screenId = GetScreenId();
            byte systemId = GetSystemId(); 

            Func<ApiResponse<LoadScreenPageResponse, SerializableDictionary<string, AutoCompleteResponse>>> fn = () =>
            {

                LoadScreenPageResponse result = _GetScreenMetaData(!AllowAnonymous());
                ApiResponse<LoadScreenPageResponse, SerializableDictionary<string, AutoCompleteResponse>> mr = new ApiResponse<LoadScreenPageResponse, SerializableDictionary<string, AutoCompleteResponse>>();
                mr.status = "success";
                mr.errorMsg = "";
                mr.data = result;
                return mr;
            };
            var ret = ProtectedCall(RestrictedApiCall(fn, systemId, screenId, "R", null), AllowAnonymous());
            return ret;

        }

        [WebMethod(EnableSession = false)]
        public ApiResponse<LoadScreenPageResponse, SerializableDictionary<string, AutoCompleteResponse>> GetScreenDdls()
        {
            int screenId = GetScreenId();
            byte systemId = GetSystemId(); 

            Func<ApiResponse<LoadScreenPageResponse, SerializableDictionary<string, AutoCompleteResponse>>> fn = () =>
            {
                LoadScreenPageResponse result = new LoadScreenPageResponse()
                {
                    Ddl = _GetScreeDdls(!AllowAnonymous()),
                };

                ApiResponse<LoadScreenPageResponse, SerializableDictionary<string, AutoCompleteResponse>> mr = new ApiResponse<LoadScreenPageResponse, SerializableDictionary<string, AutoCompleteResponse>>();
                mr.status = "success";
                mr.errorMsg = "";
                mr.data = result;
                return mr;
            };
            var ret = ProtectedCall(RestrictedApiCall(fn, systemId, screenId, "R", null), AllowAnonymous());
            return ret;
        }

        protected Tuple<List<SerializableDictionary<string, string>>, SerializableDictionary<string, string>, SerializableDictionary<string, string>, List<SerializableDictionary<string, string>>> LoadFirstOrDefault(DataTable dtScreenCriteria, bool loadFirst, string filterId, string keyId, string topN, SerializableDictionary<string, string> desiredScreenCriteria)
        {
            var NewMst = GetNewMst();
            var LastScreenCriteria = GetScreenCriteriaEX(true);
            var effectiveFilterId = GetEffectiveScreenFilterId(filterId,true);
            var effectiveDtlFilterId = GetEffectiveScreenFilterId("", false);
            Func<SerializableDictionary<string, string>, SerializableDictionary<string, SerializableDictionary<string, string>>> fn = (d) =>
            {
                if (d == null) return null;
                SerializableDictionary<string, SerializableDictionary<string, string>> x = new SerializableDictionary<string, SerializableDictionary<string, string>>();
                foreach (var o in d)
                {
                    x[o.Key] = new SerializableDictionary<string, string>() { { "LastCriteria", o.Value } };
                }
                return x;
            };
            var filledScrCriteria = fn(desiredScreenCriteria);
            var currentScrCriteria = _GetCurrentScrCriteria(new DataView(dtScreenCriteria), filledScrCriteria ?? LastScreenCriteria.data.data, true);
            var effectiveScrCriteria = _SetCurrentScrCriteria(currentScrCriteria.Item1);
            bool validFilledScrCriteria = dtScreenCriteria.Rows.Count == 0 || filledScrCriteria == null || (filledScrCriteria.Where((o, i) =>
            {
                return effectiveScrCriteria[i] == o.Value["LastCriteria"];
            }).Count() == filledScrCriteria.Count);


            if (loadFirst && (filledScrCriteria == null || validFilledScrCriteria))
            {
                var SearchList = GetSearchList(string.IsNullOrEmpty(keyId) ? "" : "**" + keyId, 2, effectiveFilterId.ToString(), desiredScreenCriteria);
                if (SearchList.data == null)
                {
                    SearchList = GetSearchList("**-1", 2, effectiveFilterId.ToString(), desiredScreenCriteria);
                }
                var firstMstId = (SearchList.data.data.FirstOrDefault() ?? new SerializableDictionary<string, string>() { { "key", "" } })["key"];
                var filteringOptions = new SerializableDictionary<string, string>() { { "CurrentScreenCriteria", new JavaScriptSerializer().Serialize(currentScrCriteria.Item2) } };
                var Mst = GetMstById(firstMstId, filteringOptions);
                var Dtl = GetDtlById(firstMstId, filteringOptions, effectiveDtlFilterId).data;
                return new Tuple<
                        List<SerializableDictionary<string, string>>
                        , SerializableDictionary<string, string>
                        , SerializableDictionary<string, string>
                        , List<SerializableDictionary<string, string>>>(
                            SearchList.data.data
                            ,new SerializableDictionary<string, string>(){
                                {"query",SearchList.data.query}
                                ,{"topN",SearchList.data.topN.ToString()}
                                ,{"skipped",SearchList.data.skipped.ToString()}
                                ,{"matchCount",SearchList.data.matchCount.ToString()}
                                ,{"total",SearchList.data.total.ToString()}
                            }
                            , Mst.data.Count > 0 ? Mst.data[0] : NewMst.data[0]
                            , Dtl);
            }
            else
            {
                int topX = 50; int.TryParse(topN, out topX);
                var SearchList = GetSearchList("", topX, effectiveFilterId.ToString(), desiredScreenCriteria);
                var Mst = NewMst;
                var Dtl = new List<SerializableDictionary<string, string>>();
                return new Tuple<
                        List<SerializableDictionary<string, string>>
                        , SerializableDictionary<string, string>
                        , SerializableDictionary<string, string>
                        , List<SerializableDictionary<string, string>>>(
                        SearchList.data.data
                        , new SerializableDictionary<string, string>(){
                            {"query",SearchList.data.query}
                            ,{"topN",SearchList.data.topN.ToString()}
                            ,{"skipped",SearchList.data.skipped.ToString()}
                            ,{"matchCount",SearchList.data.matchCount.ToString()}
                            ,{"total",SearchList.data.total.ToString()}
                        }
                        , Mst.data[0]
                        , Dtl);
            }
        }

        [WebMethod(EnableSession = false)]
        public ApiResponse<LoadScreenPageResponse, SerializableDictionary<string, AutoCompleteResponse>> LoadInitPage(SerializableDictionary<string, string> options)
        {
            string mstId = options.ContainsKey("MstId") ? options["MstId"] : "";
            bool loadFirst = (options.ContainsKey("FirstOrDefault") && options["FirstOrDefault"] == "Y") || !string.IsNullOrEmpty(mstId);
            bool skipMetaData = options.ContainsKey("SkipMetaData") && options["SkipMetaData"] == "Y";
            bool skipSupportingData = options.ContainsKey("SkipSupportingData") && options["SkipSupportingData"] == "Y";
            bool skipOnDemandData = options.ContainsKey("SkipOnDemandData") && options["SkipOnDemandData"] == "Y";
            string filterId = options.ContainsKey("FilterId") ? options["FilterId"] : "";
            string filterName = options.ContainsKey("FilterName") ? options["FilterName"] : "";
            bool refreshUsrImpr = options.ContainsKey("ReAuth") && options["ReAuth"] == "Y";
            string topN = options.ContainsKey("TopN") ? options["TopN"] : "50";
            System.Web.Script.Serialization.JavaScriptSerializer jss = new System.Web.Script.Serialization.JavaScriptSerializer();
            SerializableDictionary<string, string> currentScreenCriteria = options.ContainsKey("CurrentScreenCriteria") ? jss.Deserialize<SerializableDictionary<string, string>>(options["CurrentScreenCriteria"]) : null;

            Func<ApiResponse<LoadScreenPageResponse, SerializableDictionary<string, AutoCompleteResponse>>> fn = () =>
            {
                SwitchContext(GetSystemId(), LCurr.CompanyId, LCurr.ProjectId, true, true, refreshUsrImpr);
                LoadScreenPageResponse result = skipMetaData ? new LoadScreenPageResponse() : _GetScreenMetaData(!AllowAnonymous());
                var dtScreenCriteria = _GetScrCriteria(GetScreenId());
                var FirstOrDefault = LoadFirstOrDefault(dtScreenCriteria, loadFirst, filterId, mstId, topN, currentScreenCriteria);
                var newMst = InitMaster();
                var newDtl = InitDtl();
                result.SearchList = FirstOrDefault.Item1;
                result.SearchListParam = FirstOrDefault.Item2;
                result.Mst = FirstOrDefault.Item3;
                result.Dtl = FirstOrDefault.Item4;
                result.NewMst = newMst;
                result.NewDtl = newDtl;
                result.Ddl = skipSupportingData ? null : _GetScreeDdls(!AllowAnonymous());
                ApiResponse<LoadScreenPageResponse, SerializableDictionary<string, AutoCompleteResponse>> mr = new ApiResponse<LoadScreenPageResponse, SerializableDictionary<string, AutoCompleteResponse>>();
                mr.status = "success";
                mr.errorMsg = "";
                mr.data = result;

                return mr;
            };
            var ret = ProtectedCall(RestrictedApiCall(fn, GetSystemId(), GetScreenId(), "R", null));
            return ret;
        }

        [WebMethod(EnableSession = false)]
        public virtual ApiResponse<SerializableDictionary<string, string>, SerializableDictionary<string, AutoCompleteResponse>> GetDocZipDownload(string keyId, SerializableDictionary<string, string> options)
        {
            byte systemId = GetSystemId();
            byte dbId = GetDbId();
            int screenId = GetScreenId();
            Func<ApiResponse<SerializableDictionary<string, string>, SerializableDictionary<string, AutoCompleteResponse>>> fn = () =>
            {
                SwitchContext(systemId, LCurr.CompanyId, LCurr.ProjectId, true, true);
                string jsonCri = options.ContainsKey("CurrentScreenCriteria") ? options["CurrentScreenCriteria"] : null;
                string jsonRequestList = options.ContainsKey("CurrentScreenCriteria") ? options["CurrentScreenCriteria"] : null;
                string jsonColumnParam = options.ContainsKey("ColumnParam") ? options["ColumnParam"] : null;
                string zipFileName = options.ContainsKey("ZipFileName") ? options["ZipFileName"] : null;
                string browserTitle = options.ContainsKey("BrowserTitle") ? options["BrowserTitle"] : "something for browser title";

                ValidatedMstId(GetValidateMstIdSPName(), systemId, screenId, "**" + keyId, MatchScreenCriteria(_GetScrCriteria(screenId).DefaultView, jsonCri));
                ApiResponse<SerializableDictionary<string, string>, SerializableDictionary<string, AutoCompleteResponse>> mr = new ApiResponse<SerializableDictionary<string, string>, SerializableDictionary<string, AutoCompleteResponse>>();
                SerializableDictionary<string, AutoCompleteResponse> supportingData = new SerializableDictionary<string, AutoCompleteResponse>();
                SerializableDictionary<string, string> result = new SerializableDictionary<string, string>()
                {
                    {"_r","abcde"},
                    {"_actionUrl",GetExtUrl("~/Dnload.aspx")},
                    {"title",browserTitle},
//                    {"base64","actual content in base64"},
                    {"mimeType","application/zip"},
                    {"fileName",zipFileName ?? "abcd.zip"},
                };

                var req = MakeZipAllParam(keyId, jsonColumnParam, zipFileName ?? "abcd.zip");
                string ZipViaDirectPost = System.Configuration.ConfigurationManager.AppSettings["ZipViaDirectPost"];
                if (ZipViaDirectPost == "N")
                {
                    var f = ZipAllDoc(req);
                    result["base64"] = f.Item3 == null ? null : Convert.ToBase64String(f.Item3);
                    result["fileName"] = f.Item1;
                }
                else
                {
                    result["_r"] = req;
                }

                mr.data = result;
                mr.status = "success";
                mr.errorMsg = "";
                return mr;
            };
            var ret = ProtectedCall(RestrictedApiCall(fn, systemId, screenId, "R", null));
            return ret;
        }

        #endregion
    }


}