﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Entities.Models;
using Entities.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using UniversityAPI.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Entities.DTO;
using System.IO;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Headers;

namespace UniversityAPI.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class StudentController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        private readonly IStudentRepository _stdRepo;
        private APIResponse _response;
        
        // ok
        public StudentController(IConfiguration configuration, IStudentRepository stdRepo)
        {
            _stdRepo = stdRepo;

            _configuration = configuration;
        }

        // ok        
        [HttpGet]
        [Route("allStudents")]
        public IActionResult GetAllStudents()
        {
            var allStudents = _stdRepo.GetAllStudents();
            return Ok(allStudents);
        }

        // ok
        [HttpPost]
        [Route("addStudent")]
        public IActionResult AddStudent(Student student)
        {
            _response = new APIResponse();            
            try
            {
                // throw new Exception();
                _stdRepo.AddStudent(student);
                _response.ResponseCode = 0;
                _response.ResponseMessage = "Student Added Successfully!";
                _response.ResponseError = null;
            }
            catch(Exception ex)
            {
                _response.ResponseCode = -1;
                _response.ResponseMessage = "Server Error!";
                _response.ResponseError = ex.Message.ToString();
            }
            return Ok(_response);
        }

        // ok
        [HttpPost]
        [Route("editCourseToStd")]
        public IActionResult EditCourseToStd(List<StdToCourse> stdToCourses)
        {
            _response = new APIResponse();            
            if (_stdRepo.EditCoursesToStudent(stdToCourses))
            {
                _response.ResponseCode = 0;
                _response.ResponseMessage = "Course(s) Edited To Student Successfully!";
                _response.ResponseError = null;
            }
            else
            {
                _response.ResponseCode = -1;
                _response.ResponseMessage = "Server Error!";
                _response.ResponseError = "Server Error!";
            }      
            return Ok(_response);
        }

        // ok
        // this will load courses only assigned to respective student
        [HttpGet]
        [Route("loadCoursesForStudent/{stdId}")]
        public IActionResult LoadCoursesForStudent(int stdId)
        {
            var listOfCrs = _stdRepo.GetCoursesForStudent(stdId);
            return Ok(listOfCrs);
        }

        // ok
        // this will load assignmets only assigned to courses to respective student 
        [HttpGet]
        [Route("loadAsmtsForStudent/{stdId}")]
        public IActionResult LoadAsmtsForStudent(int stdId)
        {
            var listOfAsmts = _stdRepo.GetAsmtsForStudent(stdId);
            return Ok(listOfAsmts);
        }

        // ok
        // assignment file download
        [HttpPost, DisableRequestSizeLimit]
        [Route("downloadAsmt")]
        public async Task<IActionResult> Download(StdToAsmtDownload stdToAsmtDownload)
        {
            try
            {
                var currentDirectory = System.IO.Directory.GetCurrentDirectory();
                currentDirectory = currentDirectory + "\\StaticFiles\\Assignments";
                var file = Path.Combine(currentDirectory, stdToAsmtDownload.AsmtFileName);

                // check if file exists or not
                if (System.IO.File.Exists(file))
                {
                    var memory = new MemoryStream();
                    using (var stream = new FileStream(file, FileMode.Open))
                    {
                        await stream.CopyToAsync(memory);
                    }

                    memory.Position = 0;

                    // update db table StdToAsmt
                    _stdRepo.EditAsmtsToStudent(stdToAsmtDownload);


                    // return file for download
                    return File(memory, GetMimeType(file), stdToAsmtDownload.AsmtFileName);
                }
                else
                {
                    return BadRequest();
                }
            }
            catch (Exception e)
            {
                return BadRequest(e);
            }
        }
        private string GetMimeType(string file)
        {
            string extension = Path.GetExtension(file).ToLowerInvariant();
            switch (extension)
            {
                case ".txt": return "text/plain";
                case ".pdf": return "application/pdf";
                case ".doc": return "application/vnd.ms-word";
                case ".docx": return "application/vnd.ms-word";
                case ".xls": return "application/vnd.ms-excel";
                case ".png": return "image/png";
                case ".jpg": return "image/jpeg";
                case ".jpeg": return "image/jpeg";
                case ".gif": return "image/gif";
                case ".csv": return "text/csv";
                default: return "";
            }
        }

        // user/student area
        // ok
        // assignment submit
        [HttpPost, DisableRequestSizeLimit]
        [Route("asmtSubmit")]
        public IActionResult AsmtSubmit()
        {
            try
            {

                var stdToAsmtRequest = Request.Form["stdToAsmt"];

                string asmtStoragePath =_configuration.GetSection("AsmtSubmitLocation").GetSection("Path").Value;

                // unique random number to edit file name
                var guid = Guid.NewGuid();
                var bytes = guid.ToByteArray();
                var rawValue = BitConverter.ToInt64(bytes, 0);
                var inRangeValue = Math.Abs(rawValue) % DateTime.MaxValue.Ticks;


                var file = Request.Form.Files[0];
                var pathToSave = Path.Combine(Directory.GetCurrentDirectory(), asmtStoragePath);

                if (file.Length > 0)
                {
                    var fileName = inRangeValue + "_" + ContentDispositionHeaderValue.Parse(file.ContentDisposition).FileName.Trim('"');
                    var fullPath = Path.Combine(pathToSave, fileName);

                    using (var stream = new FileStream(fullPath, FileMode.Create))
                    {
                        file.CopyTo(stream);
                    }

                    AsmtSubmitVM asmtSubmitVM = new AsmtSubmitVM();
                    asmtSubmitVM.AsmtSubmitFilePath = asmtStoragePath;
                    asmtSubmitVM.AsmtSubmitFileName = fileName;
                    asmtSubmitVM.AsmtSubmitDate = DateTime.Now;
                    asmtSubmitVM.AsmtLinkStatus = AsmtLinkStatus.AsmtSubmitted;
                    asmtSubmitVM.AssignmentId = Convert.ToInt32(Request.Form["stdToAsmt.assignmentId"]);
                    asmtSubmitVM.StudentId = Convert.ToInt32(Request.Form["stdToAsmt.studentId"]);                    

                    asmtSubmitVM = _stdRepo.AsmtSubmit(asmtSubmitVM);
                    return Ok(new { asmtSubmitVM });

                }
                else
                {
                    return BadRequest();
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex}");
            }
        }


    }
}
