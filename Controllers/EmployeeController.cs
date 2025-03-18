using Microsoft.AspNetCore.Mvc;
using Oracle.ManagedDataAccess.Client;
using System.Collections.Generic;
using System.Data;

[ApiController]
[Route("api/[controller]")]
public class EmployeeController : ControllerBase
{
    //private readonly string _connectionString = "User Id=NG_NTTML;Password=NGI_NTTML;Data Source=(DESCRIPTION=(ADDRESS_LIST=(ADDRESS=(PROTOCOL=TCP)(HOST=103.4.116.121)(PORT=1521)))(CONNECT_DATA=(SERVICE_NAME=orcl)));";
    private readonly string _connectionString = "User Id=NG_TFL;Password=NGI_TFL;Data Source=(DESCRIPTION=(ADDRESS_LIST=(ADDRESS=(PROTOCOL=TCP)(HOST=103.4.116.121)(PORT=1522)))(CONNECT_DATA=(SERVICE_NAME=orcl)));";

    [HttpGet("{empCode}")]
    public IActionResult GetEmployee(string empCode)
    {
        using (var connection = new OracleConnection(_connectionString))
        {
            string query = @"
                SELECT E_O.EMP_ID, E_O.EMP_CODE, E_O.ERP_CODE, E_O.EMP_NAME, 
                       D.DESIGNATION_NAME, DP.DEPARTMENT_NAME, SEC.SECTION_NAME, 
                       F.FLOOR_NAME, E_C.EMP_CATEGORY_NAME, 
                       E_P.SEX AS GENDER, E_P.NATIONAL_ID,
                       E_O.DATE_OF_JOINING, 
                       ROUND(DECODE(S_R.RULE_BASIC, 50, E_O.GROSS / 2, 
                            ((E_O.GROSS - (S_R.RULE_TRANSPORT + S_R.RULE_MEDICAL + S_R.RULE_FOOD)) / 1.55))
                       , 0) AS BASIC, 
                       E_O.GROSS, E_O.ACCOUNT_NO, E_O.MOBILE_BANK_ACC_NO, E_O.EMP_STATUS, U.UNIT_NAME
                FROM Emp_official E_O
                LEFT JOIN DESIGNATION D ON E_O.DESIGNATION_ID = D.DESIGNATION_ID
                LEFT JOIN DEPARTMENT DP ON E_O.DEPARTMENT_ID = DP.DEPARTMENT_ID
                LEFT JOIN SECTION SEC ON E_O.SECTION_ID = SEC.SECTION_ID
                LEFT JOIN FLOOR F ON E_O.FLOOR_ID = F.FLOOR_ID
                LEFT JOIN UNIT U ON E_O.UNIT_ID = U.UNIT_ID
                LEFT JOIN EMP_CATEGORY E_C ON E_O.EMP_CATEGORY_ID = E_C.EMP_CATEGORY_ID
                LEFT JOIN EMP_PERSONAL E_P ON E_O.EMP_ID = E_P.EMP_ID
                LEFT JOIN SALARY_RULE_INFO S_R ON E_O.RULE_ID = S_R.RULE_ID
                WHERE E_O.EMP_CODE = :empCode";

            using (var command = new OracleCommand(query, connection))
            {
                command.Parameters.Add(new OracleParameter("empCode", empCode));
                connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        var data = new Dictionary<string, object>();
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            data[reader.GetName(i)] = reader[i];
                        }
                        return Ok(data);
                    }
                    else
                    {
                        return NotFound(new { message = "Employee not found" });
                    }
                }
            }
        }
    }

    // URL-http://localhost:5085/api/Employee/active_emp?page=1&pageSize=10
    [HttpGet("active_emp/")]
    public IActionResult GetEmployeesByStatus(int page = 1, int pageSize = 10)
    {
        List<Dictionary<string, object>> employees = new List<Dictionary<string, object>>();

        using (var connection = new OracleConnection(_connectionString))
        {
            string query = @"
                SELECT * FROM (
                    SELECT E_O.EMP_ID, E_O.EMP_CODE, E_O.ERP_CODE, E_O.EMP_NAME, 
                           D.DESIGNATION_NAME, DP.DEPARTMENT_NAME, SEC.SECTION_NAME, 
                           F.FLOOR_NAME, E_C.EMP_CATEGORY_NAME, 
                           E_P.SEX AS GENDER, E_P.NATIONAL_ID,
                           E_O.DATE_OF_JOINING, 
                           ROUND(DECODE(S_R.RULE_BASIC, 50, E_O.GROSS / 2, 
                                ((E_O.GROSS - (S_R.RULE_TRANSPORT + S_R.RULE_MEDICAL + S_R.RULE_FOOD)) / 1.55))
                           , 0) AS BASIC, 
                           E_O.GROSS, E_O.ACCOUNT_NO, E_O.MOBILE_BANK_ACC_NO, E_O.EMP_STATUS, U.UNIT_NAME,
                           ROWNUM AS row_num
                    FROM Emp_official E_O
                    LEFT JOIN DESIGNATION D ON E_O.DESIGNATION_ID = D.DESIGNATION_ID
                    LEFT JOIN DEPARTMENT DP ON E_O.DEPARTMENT_ID = DP.DEPARTMENT_ID
                    LEFT JOIN SECTION SEC ON E_O.SECTION_ID = SEC.SECTION_ID
                    LEFT JOIN FLOOR F ON E_O.FLOOR_ID = F.FLOOR_ID
                    LEFT JOIN UNIT U ON E_O.UNIT_ID = U.UNIT_ID
                    LEFT JOIN EMP_CATEGORY E_C ON E_O.EMP_CATEGORY_ID = E_C.EMP_CATEGORY_ID
                    LEFT JOIN EMP_PERSONAL E_P ON E_O.EMP_ID = E_P.EMP_ID
                    LEFT JOIN SALARY_RULE_INFO S_R ON E_O.RULE_ID = S_R.RULE_ID
                    WHERE E_O.EMP_STATUS = 'Active'
                ) WHERE row_num BETWEEN :startRow AND :endRow";

            using (var command = new OracleCommand(query, connection))
            {
                int startRow = (page - 1) * pageSize + 1;
                int endRow = page * pageSize;

                //command.Parameters.Add(new OracleParameter("status", status));
                command.Parameters.Add(new OracleParameter("startRow", startRow));
                command.Parameters.Add(new OracleParameter("endRow", endRow));

                connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var employee = new Dictionary<string, object>();
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            employee[reader.GetName(i)] = reader[i];
                        }
                        employees.Add(employee);
                    }
                }
            }
        }

        if (employees.Count > 0)
            return Ok(employees);
        else
            return NotFound(new { message = $"No employees found " });
    }


    [HttpGet("attendance/{empCode}")]
    public IActionResult GetAttendanceByEmpCode(string empCode, [FromQuery] string fromDate, [FromQuery] string toDate)
    {
        List<Dictionary<string, object>> attendanceRecords = new List<Dictionary<string, object>>();

        using (var connection = new OracleConnection(_connectionString))
        {
            string query = @"
        SELECT A.EMP_ID, 
            E_O.EMP_CODE, 
            E_O.ERP_CODE, 
            E_O.EMP_NAME, 
            A.ATTD_DATE, 
            A.IN_TIME, 
            A.OUT_TIME, 
            A.WORK_HOUR, 
            A.STATUS, 
            A.OVER_TIME, 
            A.NIGHT_STATUS, 
            E_O.DATE_OF_JOINING, 
            E_O.ACCOUNT_NO, 
            E_O.MOBILE_BANK_ACC_NO, 
            E_O.EMP_STATUS,
            U.UNIT_NAME
        FROM ATTENDANCE_DETAILS A
        JOIN Emp_official E_O ON A.EMP_ID = E_O.EMP_ID
        LEFT JOIN UNIT U ON E_O.UNIT_ID = U.UNIT_ID
        WHERE E_O.EMP_CODE = :empCode
        AND A.ATTD_DATE BETWEEN TO_DATE(:fromDate, 'DD-MON-YY') AND TO_DATE(:toDate, 'DD-MON-YY')";

            using (var command = new OracleCommand(query, connection))
            {
                command.Parameters.Add(new OracleParameter("empCode", empCode));
                command.Parameters.Add(new OracleParameter("fromDate", fromDate));
                command.Parameters.Add(new OracleParameter("toDate", toDate));

                connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var record = new Dictionary<string, object>();
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            record[reader.GetName(i)] = reader[i];
                        }
                        attendanceRecords.Add(record);
                    }
                }
            }
        }

        if (attendanceRecords.Count > 0)
            return Ok(attendanceRecords);
        else
            return NotFound(new { message = "No attendance records found for this employee in the given date range." });
    }


    [HttpGet("attendance/active")]
    public IActionResult GetActiveEmployeesAttendance([FromQuery] string fromDate, [FromQuery] string toDate, int page = 1, int pageSize = 10)
    {
        var attendanceRecords = new List<Dictionary<string, object>>();

        using var connection = new OracleConnection(_connectionString);
        string query = @"
        SELECT * FROM (
            SELECT A.EMP_ID, 
                E_O.EMP_CODE, 
                E_O.ERP_CODE, 
                E_O.EMP_NAME, 
                A.ATTD_DATE, 
                A.IN_TIME, 
                A.OUT_TIME, 
                A.WORK_HOUR, 
                A.STATUS, 
                A.OVER_TIME, 
                A.NIGHT_STATUS, 
                E_O.DATE_OF_JOINING, 
                E_O.ACCOUNT_NO, 
                E_O.MOBILE_BANK_ACC_NO, 
                E_O.EMP_STATUS,
                U.UNIT_NAME,
                ROWNUM AS row_num
            FROM ATTENDANCE_DETAILS A
            JOIN Emp_official E_O ON A.EMP_ID = E_O.EMP_ID
            LEFT JOIN UNIT U ON E_O.UNIT_ID = U.UNIT_ID
            WHERE E_O.EMP_STATUS = 'Active'
            AND A.ATTD_DATE BETWEEN TO_DATE(:fromDate, 'DD-MON-YY') AND TO_DATE(:toDate, 'DD-MON-YY')
        ) WHERE row_num BETWEEN :startRow AND :endRow";

        using var command = new OracleCommand(query, connection);
        int startRow = (page - 1) * pageSize + 1;
        int endRow = page * pageSize;

        command.Parameters.Add(new OracleParameter("fromDate", fromDate));
        command.Parameters.Add(new OracleParameter("toDate", toDate));
        command.Parameters.Add(new OracleParameter("startRow", startRow));
        command.Parameters.Add(new OracleParameter("endRow", endRow));

        connection.Open();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var record = new Dictionary<string, object>();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                record[reader.GetName(i)] = reader[i];
            }
            attendanceRecords.Add(record);
        }

        if (attendanceRecords.Count > 0)
            return Ok(attendanceRecords);
        else
            return NotFound(new { message = "No attendance records found for active employees in the given date range." });
    }


    //[HttpPost("pos/")]
    //public IActionResult InsertEmployeePurchase(
    //[FromBody] List<EmpPosDto> empPosList,
    //[FromHeader(Name = "X-API-KEY")] string apiKey)
    //{
    //    const string validApiKey = "TOKWt9Y8QuDsUOFS2qlWro0h9ceJL4zO"; 

    //    if (apiKey != validApiKey)
    //    {
    //        return Unauthorized(new { message = "Invalid API Key" });
    //    }

    //    using (var connection = new OracleConnection(_connectionString))
    //    {
    //        connection.Open();
    //        using (var transaction = connection.BeginTransaction())
    //        {
    //            try
    //            {
    //                string query = @"
    //            INSERT INTO EMP_POS (EMP_CODE, AMOUNT, PURCHASE_DATE)
    //            VALUES (:empCode, :amount, TO_DATE(:purchaseDate, 'YYYY-MM-DD'))";

    //                foreach (var empPos in empPosList)
    //                {
    //                    using (var command = new OracleCommand(query, connection))
    //                    {
    //                        command.Parameters.Add(new OracleParameter("empCode", empPos.EMP_CODE));
    //                        command.Parameters.Add(new OracleParameter("amount", empPos.AMOUNT));
    //                        command.Parameters.Add(new OracleParameter("purchaseDate", empPos.PURCHASE_DATE.ToString("yyyy-MM-dd")));

    //                        command.ExecuteNonQuery();
    //                    }
    //                }

    //                transaction.Commit();
    //                return Ok(new { message = "Employee purchase records inserted successfully." });
    //            }
    //            catch (Exception ex)
    //            {
    //                transaction.Rollback();
    //                return StatusCode(500, new { message = "An error occurred while inserting records.", error = ex.Message });
    //            }
    //        }
    //    }
    //}
    [HttpPost("pos/")]
    public IActionResult InsertEmployeePurchase(
    [FromBody] List<EmpPosDto> empPosList,
    [FromHeader(Name = "X-API-KEY")] string apiKey)
    {
        const string validApiKey = "TOKWt9Y8QuDsUOFS2qlWro0h9ceJL4zO";

        if (apiKey != validApiKey)
        {
            return Unauthorized(new { message = "Invalid API Key" });
        }

        int insertedRecords = 0;
        var skippedRecords = new List<string>();

        using (var connection = new OracleConnection(_connectionString))
        {
            connection.Open();
            using (var transaction = connection.BeginTransaction())
            {
                try
                {
                    string query = @"
                INSERT INTO EMP_POS (EMP_CODE, AMOUNT, PURCHASE_DATE)
                VALUES (:empCode, :amount, TO_DATE(:purchaseDate, 'YYYY-MM-DD'))";

                    foreach (var empPos in empPosList)
                    {
                        // Skip if amount is zero
                        if (empPos.AMOUNT == 0)
                        {
                            skippedRecords.Add($"EMP_CODE: {empPos.EMP_CODE} skipped (Amount is 0)");
                            continue;
                        }

                        // Check if EMP_CODE exists in the Emp_official table
                        using (var checkCommand = new OracleCommand("SELECT COUNT(*) FROM Emp_official WHERE EMP_CODE = :empCode", connection))
                        {
                            checkCommand.Parameters.Add(new OracleParameter("empCode", empPos.EMP_CODE));
                            int count = Convert.ToInt32(checkCommand.ExecuteScalar());

                            if (count == 0)
                            {
                                skippedRecords.Add($"EMP_CODE: {empPos.EMP_CODE} skipped (Not found in Emp_official)");
                                continue;
                            }
                        }

                        // Insert if all conditions are met
                        using (var command = new OracleCommand(query, connection))
                        {
                            command.Parameters.Add(new OracleParameter("empCode", empPos.EMP_CODE));
                            command.Parameters.Add(new OracleParameter("amount", empPos.AMOUNT));
                            command.Parameters.Add(new OracleParameter("purchaseDate", empPos.PURCHASE_DATE.ToString("yyyy-MM-dd")));

                            command.ExecuteNonQuery();
                            insertedRecords++;
                        }
                    }

                    transaction.Commit();

                    // Different messages based on insert status
                    if (insertedRecords > 0)
                    {
                        return Ok(new
                        {
                            message = "Employee purchase records inserted successfully.",
                            insertedRecords,
                            skippedRecords
                        });
                    }
                    else
                    {
                        return BadRequest(new
                        {
                            message = "No records inserted. All were skipped.",
                            insertedRecords,
                            skippedRecords
                        });
                    }
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return StatusCode(500, new
                    {
                        message = "An error occurred while inserting records.",
                        error = ex.Message
                    });
                }
            }
        }
    }




    public class EmpPosDto
    {
        public string EMP_CODE { get; set; } = string.Empty;
        public decimal AMOUNT { get; set; }
        public DateTime PURCHASE_DATE { get; set; }
    }




}
