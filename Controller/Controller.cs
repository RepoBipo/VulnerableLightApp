﻿using System.Data;
using System.Security.Claims;
using System.Text;
using System.Xml;
using Microsoft.AspNetCore.DataProtection;
using Newtonsoft.Json;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Net.Http.Headers;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System;
using System.Threading;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using static System.Net.Mime.MediaTypeNames;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Diagnostics.Tracing;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.AspNetCore.Http;
using System.Security.Cryptography;
using System.Linq.Dynamic.Core;
using static VulnerableWebApplication.VLAController.VLAController;
using Microsoft.AspNetCore.Mvc;
using System.Xml.Linq;
using System.Xml.Xsl;
using VulnerableWebApplication.VLAModel;

namespace VulnerableWebApplication.VLAController
{
    public class VLAController
    {
        public static object VulnerableHelloWorld(string FileName = "francais")
        {
            /*
             Retourne le contenu du fichier correspondant à la langue choisie par l'utilisateur
            */
            if (FileName.IsNullOrEmpty()) FileName = "francais";
            string Content = File.ReadAllText(FileName.Replace("../", "").Replace("..\\", ""));

            return "{\"success\":" + Content + "}";
        }

        public static object VulnerableDeserialize(string Json)
        {
            /*
            Deserialise les données JSON passées en paramètre et s'assure que le fichier "ReadOnly.txt" soit en lecture seule
            */
            string ROFile = "ReadOnly.txt";
            Json = Json.Replace("Framework", "").Replace("Token", "").Replace("Cmd", "").Replace("powershell", "").Replace("http", "");
            if (!File.Exists(ROFile)) File.WriteAllText(ROFile, new Guid().ToString());
            File.SetAttributes(ROFile, FileAttributes.ReadOnly);
            JsonConvert.DeserializeObject<object>(Json, new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.All });

            return "{\"" + ROFile + "\":\"" + File.GetAttributes(ROFile).ToString() + "\"}";
        }

        public static string VulnerableXmlParser(string Xml)
        {
            /*
            Traite les données XML passées en paramètre et retourne son contenu
            */
            try
            {
                var Xsl = XDocument.Parse(Xml);
                var MyXslTrans = new XslCompiledTransform(enableDebug: true);
                var Settings = new XsltSettings();
                MyXslTrans.Load(Xsl.CreateReader(), Settings, null);
                var DocReader = XDocument.Parse("<doc></doc>").CreateReader();

                var Sb = new StringBuilder();
                var DocWriter = XmlWriter.Create(Sb, new XmlWriterSettings() { ConformanceLevel = ConformanceLevel.Fragment });
                MyXslTrans.Transform(DocReader, DocWriter);

                return Sb.ToString();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                Xml = Xml.Replace("Framework", "").Replace("Token", "").Replace("Cmd", "").Replace("powershell", "").Replace("http", "");
                XmlReaderSettings ReaderSettings = new XmlReaderSettings();
                ReaderSettings.DtdProcessing = DtdProcessing.Parse;
                ReaderSettings.XmlResolver = new XmlUrlResolver();
                ReaderSettings.MaxCharactersFromEntities = 6000;

                using (MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(Xml)))
                {
                    XmlReader Reader = XmlReader.Create(stream, ReaderSettings);
                    var XmlDocument = new XmlDocument();
                    XmlDocument.XmlResolver = new XmlUrlResolver();
                    XmlDocument.Load(Reader);

                    return XmlDocument.InnerText;
                }
            }
        }

        public static string VulnerableLogs(string Str, string LogFile)
        {
            /*
            Enregistre la chaine de caractères passée en paramètre dans le fichier de journalisation
            */
            if (!File.Exists(LogFile)) File.WriteAllText(LogFile, Data.GetLogPage());
            string Page = File.ReadAllText(LogFile).Replace("</body>", "<p>" + Str + "<p><br>" + Environment.NewLine + "</body>");
            File.WriteAllText(LogFile, Page);

            return "{\"success\":true}";
        }

        public static async Task<string> VulnerableQuery(string User, string Passwd, string Secret, string LogFile)
        {
            /*
            Authentifie les utilisateurs par login et mot de passe, et renvoie un token JWT si l'authentification a réussie
            */
            SHA256 Sha256Hash = SHA256.Create();
            byte[] Bytes = Sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(Passwd));            
            StringBuilder stringbuilder = new StringBuilder();
            for (int i = 0; i < Bytes.Length; i++) stringbuilder.Append(Bytes[i].ToString("x2"));
            string Hash = stringbuilder.ToString();

            VulnerableLogs("login attempt for:\n" + User + "\n" + Passwd + "\n", LogFile);
            var DataSet = Data.GetDataSet();
            var Result = DataSet.Tables[0].Select("Passwd = '" + Hash + "' and User = '" + User + "'");

            return Result.Length > 0 ? "Bearer " + VulnerableGenerateToken(User, Secret) : "{\"success\":false}";
        }

        public static string VulnerableGenerateToken(string User, string Secret)
        {
            /*
            Retourne un token JWT signé pour l'utilisateur passé en paramètre
            */
            var TokenHandler = new JwtSecurityTokenHandler();
            var Key = Encoding.ASCII.GetBytes(Secret);
            var TokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[] { new Claim("Id", User) }),
                Expires = DateTime.UtcNow.AddDays(365),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(Key), SecurityAlgorithms.HmacSha256Signature)
            };
            var Token = TokenHandler.CreateToken(TokenDescriptor);

            return TokenHandler.WriteToken(Token);
        }

        public static bool VulnerableValidateToken(string Token, string Secret)
        {
            /*
            Vérifie la validité du token JWT passé en paramètre
            */
            var TokenHandler = new JwtSecurityTokenHandler();
            var Key = Encoding.ASCII.GetBytes(Secret);
            bool Result = true;
            try
            {
                var JwtSecurityToken = TokenHandler.ReadJwtToken(Token);
                if (JwtSecurityToken.Header.Alg == "HS256" && JwtSecurityToken.Header.Typ == "JWT")
                {
                    TokenHandler.ValidateToken(Token, new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Key),
                        ValidateIssuer = false,
                        ValidateAudience = false,
                    }, out SecurityToken validatedToken);

                    var JwtToken = (JwtSecurityToken)validatedToken;
                }
            }
            catch { Result = false; }

            return Result;
        }

        public static async Task<string> VulnerableWebRequest(string Uri = "https://localhost:3000/")
        {
            /*
            Effectuer une requête web vers Localhost
            */
            if (Uri.IsNullOrEmpty()) Uri = "https://localhost:3000/";
            if (Regex.IsMatch(Uri, @"^https://localhost"))
            {
                using HttpClient Client = new();
                Client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));

                var Resp = await exec(Client, Uri);
                static async Task<string> exec(HttpClient client, string uri)
                {
                    var Result = client.GetAsync(uri);
                    Result.Result.EnsureSuccessStatusCode();
                    return Result.Result.StatusCode.ToString();
                }
                return "{\"Result\":" + Resp + "}";
            }
            else return "{\"Result\": \"Fordidden\"}";
        }

        public static string VulnerableObjectReference(int Id, string Token, string Secret)
        {
            /*
            Retourne les informations liées à l'ID de l'utilisateur
            */
            if (!VulnerableValidateToken(Token, Secret)) return "{\"" + Id + "\":\"Forbidden\"}";
            else
            {
                List<Employee> Employees = Data.GetEmployees();
                return "{\"" + Id + "\":\"" + Employees.Where(x => Id == x.Id)?.FirstOrDefault()?.Address + "\"}";
            }
        }

        public static string VulnerableCmd(string UserStr)
        {
            /*
            Effectue une requête DNS pour le FQDN passé en paramètre
            */
            string Result;
            if (Regex.Match(UserStr, @"[a-zA-Z0-9][a-zA-Z0-9-]{1,10}\.[a-zA-Z]{2,3}$|[a-zA-Z0-9][a-zA-Z0-9-]{1,10}\.[a-zA-Z]{2,3}[& a-zA-Z]{2,10}$").Success)
            {
                Process Cmd = new Process();
                Cmd.StartInfo.FileName = "Cmd.exe";
                Cmd.StartInfo.RedirectStandardInput = true;
                Cmd.StartInfo.RedirectStandardOutput = true;
                Cmd.StartInfo.CreateNoWindow = true;
                Cmd.StartInfo.UseShellExecute = false;
                Cmd.Start();
                Cmd.WaitForExit(200);
                Cmd.StandardInput.WriteLine("nslookup " + UserStr);
                Cmd.StandardInput.Flush();
                Cmd.StandardInput.Close();

                Result = "{\"Result\":\"" + Cmd.StandardOutput.ReadToEnd() + "\"}";
            }
            else Result = "{\"Result\":\"ERROR\"}";

            return Result;
        }

        public static unsafe string VulnerableBuffer(string UserStr)
        {
            /*
            Copie une chaine de caractère
            */
            char* Ptr = stackalloc char[50], Str = Ptr + 50;
            foreach (var c in UserStr) *Ptr++ = c;

            return new string(Str);
        }

        public static string VulnerableCodeExecution(string UserStr)
        {
            /*
            Retourne le résultat de l'opération mathématique sur le chiffre donné en paramètre
            */
            string Result = "null";
            if (UserStr.Length < 40 && !UserStr.Contains("class") && !UserStr.Contains("=") && !UserStr.Contains("using"))
            {
                try { Result = CSharpScript.EvaluateAsync("System.Math.Pow(2, " + UserStr + ")")?.Result?.ToString(); }
                catch (Exception e) { Result = e.ToString(); }
            }

            return Result + VulnerableBuffer(UserStr);
        }

        public static string VulnerableNoSQL(string UserStr)
        {
            /*
            Retourne le résultat de la requête NoSQL fournie en paramètre
            */
            if (UserStr.Length > 250) return "{\"Result\": \"Fordidden\"}";
            List<Employee> Employees = Data.GetEmployees();
            var Query = Employees.AsQueryable();
            var Result = Query.Where(UserStr);

            return Result.ToArray().ToString();
        }

        public static string VulnerableAdminDashboard(string Token, string Header, string Secret, string LogFile)
        {
            /*
            Authentifie l'utilisateur et son IP puis renvoie le journal d'événements
            */
            if (!VulnerableValidateToken(Token, Secret)) return "{\"Token\":\"Forbidden\"}";
            if (!Header.Contains("10.256.256.256")) return "{\"IP\":\"Forbidden\"}";
            VulnerableLogs("admin logged with : " + Token + Header, LogFile);

            return "{\"IP\":\"" + File.ReadAllText(LogFile) + "\"}";
        }

        public static async Task<IResult> VulnerableHandleFileUpload(IFormFile UserFile)
        {
            /*
            Permet l'upload d'image au format svg
            */
            if (UserFile.FileName.EndsWith(".svg")) 
            {
                using var Stream = File.OpenWrite(UserFile.FileName);
                await UserFile.CopyToAsync(Stream);

                return Results.Ok(UserFile.FileName);
            }
            else return Results.BadRequest();

        }
    }
}
