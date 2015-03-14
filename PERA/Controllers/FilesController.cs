﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Description;
using PERA.Models;
using System.Threading.Tasks;
using System.IO;
using System.Web;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Text;
using Excel;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

public struct columns
{
    public int invoice;
    public int QL;
}




namespace PERA.Controllers
{
    public class FilesController : ApiController
    {

        private PERAContext db = new PERAContext();

        Dictionary<int, int> invoice_token_columns = new Dictionary<int, int>()
        {
            {5,3},
            {6,3},
            {3,3},
            {4,3},
            {13,3},
            {11,3},
            {15,3},
            {14,3},
            {1,0},
            {2,1},
            {16,3},
        };

        Dictionary<int, int> invoice_name_columns = new Dictionary<int, int>()
        {

        };

        [HttpPost] // This is from System.Web.Http, and not from System.Web.Mvc
        public async Task<HttpResponseMessage> Upload()
        {
            if (!Request.Content.IsMimeMultipartContent())
            {
                this.Request.CreateResponse(HttpStatusCode.UnsupportedMediaType);
            }

            var provider = GetMultipartProvider();
            var result = await Request.Content.ReadAsMultipartAsync(provider);

            Invoice fileUploadObj = GetFormData(result);
            
            //Invoice newInvoice = JsonConvert.DeserializeObject<Invoice>(result.FormData);
            Trace.WriteLine(fileUploadObj.InvoiceID);


            foreach(var file in result.FileData)
            {

                // On upload, files are given a generic name like "BodyPart_26d6abe1-3ae1-416a-9429-b35f15e6e5d5"
                // so this is how you can get the original file name
                var originalFileName = GetDeserializedFileName(file);

                //System.Diagnostics.Debug.WriteLine(originalFileName);
           
                var uploadedFileInfo = new FileInfo(file.LocalFileName);
                System.Diagnostics.Debug.WriteLine(uploadedFileInfo);

                FileStream stream = File.Open(file.LocalFileName, FileMode.Open, FileAccess.Read);

                // we use two separate packages to parse excel files, due to errors in ExcelDataReader OpenXmlReader

                if (originalFileName.EndsWith(".xls"))
                {
                    System.Diagnostics.Debug.WriteLine("xls");
                    // Reading from a binary Excel file ('97-2003 format; *.xls)
                    XLSParser(stream, originalFileName);
                }

                else if (originalFileName.EndsWith(".xlsx"))
                {
                    //stream.Close();
                    System.Diagnostics.Debug.WriteLine("xlsx");
                    XLSParser(stream, originalFileName);
                }
            }


            Trace.WriteLine("hello");
            // Remove this line as well as GetFormData method if you're not
            // sending any form data with your upload request


            // Through the request response you can return an object to the Angular controller
            // You will be able to access this in the .success callback through its data attribute
            // If you want to send something to the .error callback, use the HttpStatusCode.BadRequest instead
            var returnData = "ReturnTest";
            return this.Request.CreateResponse(HttpStatusCode.OK, new { returnData });
        }

        private void XLSXParser(string filename)
        {

            SpreadsheetDocument spreadsheetDocument = SpreadsheetDocument.Open(filename, false);
            WorkbookPart workbookPart = spreadsheetDocument.WorkbookPart;
            // Iterate through all WorksheetParts
            foreach (WorksheetPart worksheetPart in workbookPart.WorksheetParts)
            {
                OpenXmlPartReader reader = new OpenXmlPartReader(worksheetPart);
                string text;
                string rowNum;
                while (reader.Read())
                {
                   if (reader.ElementType == typeof(Cell))
                    {
                        Cell c = (Cell)reader.LoadCurrentElement();

                        string cellValue;

                        if (c.DataType != null && c.DataType == CellValues.SharedString)
                        {
                            SharedStringItem ssi = workbookPart.SharedStringTablePart.SharedStringTable.Elements<SharedStringItem>().ElementAt(int.Parse(c.CellValue.InnerText));

                            cellValue = ssi.Text.Text;
                        }
                        else
                        {
                            cellValue = c.CellValue.InnerText;
                        }

                        Console.Out.Write("{0}: {1} ", c.CellReference, cellValue);
                    }
                          

                    System.Diagnostics.Debug.WriteLine("xlsx for loop");
                    Trace.WriteLine("row");
                    do
                    {

                        if (reader.HasAttributes)
                        {
                            rowNum = reader.Attributes.First(a => a.LocalName == "r").Value;
                            Trace.WriteLine("rowNum: " + rowNum);
                        }

                    } while (reader.ReadNextSibling()); // Skip to the next row

                    break; // We just looped through all the rows so no 
                    // need to continue reading the worksheet
                }

                if (reader.ElementType != typeof(Worksheet))
                    reader.Skip();

                reader.Close();
            }        
        }
        

        private void XLSParser(FileStream stream, string name)
        {
            System.Diagnostics.Debug.WriteLine("begin excel parser");
            IExcelDataReader excelReader = null;
            


            if (name.EndsWith(".xls"))
            {
                excelReader = ExcelReaderFactory.CreateBinaryReader(stream);
            }

            else if (name.EndsWith(".xlsx"))
            {
                //stream.Close();
                System.Diagnostics.Debug.WriteLine("xlsx");
                excelReader = ExcelReaderFactory.CreateOpenXmlReader(stream);
            }

            if (excelReader == null)
            {
                System.Diagnostics.Debug.WriteLine("excel reader null");
            }
            // Create column names from first row
            excelReader.IsFirstRowAsColumnNames = true;

            // The result of each spreadsheet will be created in the result.Tables
            System.Diagnostics.Debug.WriteLine("creating dataset..");
            DataSet result = excelReader.AsDataSet();
            System.Diagnostics.Debug.WriteLine(result);

            List<ParkerReportTeamMember> teamMembers = new List<ParkerReportTeamMember>();
            var table = result.Tables[0];
            System.Diagnostics.Debug.WriteLine("begin for loop");
                foreach (DataRow row in table.Rows)
                {
                    // if this row is the headings, skip this row
                    System.Diagnostics.Debug.WriteLine("loop");
                    if(row == null )
                    {
                        System.Diagnostics.Debug.WriteLine("null");
                        continue;
                    }
                    //System.Diagnostics.Debug.WriteLine("b4");
                    if(row[0] is String)
                    {
                        System.Diagnostics.Debug.WriteLine(row[0]);
                        continue;
                    }
                    System.Diagnostics.Debug.WriteLine("teamMember");
                    ParkerReportTeamMember teamMember = new ParkerReportTeamMember();

                    string fullName = (string)row[2];

                    string[] names = fullName.Split(',');   //split the name
                    string firstName, lastName;
                    if (names.Length < 2)
                    {
                        firstName = names[0];
                        lastName = "";
                    }
                    else
                    {
                        firstName = names[1];
                        lastName = names[0];
                    }

                    //string cardNumber = (string)row[3];
                    double cardNumberD = 0;
                    int cardNumber = 0;
                    for(int i = 3; i < 8; i++)
                    {
                        if(row[i] is System.DBNull)
                            continue;
                        //System.Diagnostics.Debug.WriteLine(row[i].GetType());
                        cardNumberD = (System.Double)row[i];
                        cardNumber = Convert.ToInt32(cardNumberD);
                    }


                    teamMember.FirstName = firstName;
                    teamMember.LastName = lastName;
                    teamMember.BusinessHoursTokenID = cardNumber;

                    teamMembers.Add(teamMember);

                    db.ParkerReportTeamMembers.Add(teamMember);
                    db.SaveChanges();

                      /*
                    foreach (DataColumn column in table.Columns)
                    {
                        System.Diagnostics.Debug.WriteLine(row[column]);
                    }*/
                                       
                } // end for columns
            //}  end for tables

        }


        // You could extract these two private methods to a separate utility class since
        // they do not really belong to a controller class but that is up to you
        private MultipartFormDataStreamProvider GetMultipartProvider()
        {
            // IMPORTANT: replace "(tilde)" with the real tilde character
            // (our editor doesn't allow it, so I just wrote "(tilde)" instead)
            var uploadFolder = "~/App_Data/Tmp/FileUploads"; // you could put this to web.config
            var root = HttpContext.Current.Server.MapPath(uploadFolder);
            Directory.CreateDirectory(root);
            return new MultipartFormDataStreamProvider(root);
        }

        // Extracts Request FormatData as a strongly typed model
        private Invoice GetFormData(MultipartFormDataStreamProvider result)
        {
            if (result.FormData.HasKeys())
            {
                var unescapedFormData = Uri.UnescapeDataString(result.FormData
                    .GetValues(0).FirstOrDefault() ?? String.Empty);
                if (!String.IsNullOrEmpty(unescapedFormData))
                {
                    System.Diagnostics.Debug.WriteLine("inside if form data");
                    return JsonConvert.DeserializeObject<Invoice>(result.FormData.GetValues(0).FirstOrDefault());
                }
                    
            }

            return null;
        }

        private string GetDeserializedFileName(MultipartFileData fileData)
        {
            var fileName = GetFileName(fileData);
            return JsonConvert.DeserializeObject(fileName).ToString();
        }

        public string GetFileName(MultipartFileData fileData)
        {
            return fileData.Headers.ContentDisposition.FileName;
        }
    }
}