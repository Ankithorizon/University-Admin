﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Entities.DTO
{
    public class AsmtFacDeptVM
    {
        public int AssignmentId { get; set; }
        public string Title { get; set; }
        public string Details { get; set; }
        public int FacultyId { get; set; }
        public string FacultyName { get; set; }
        public int DepartmentId { get; set; }
        public string DepartmentName { get; set; }

    }
}
