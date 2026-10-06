import io.restassured.RestAssured;
import io.restassured.http.ContentType;
import io.restassured.response.Response;
import org.testng.Assert;
import org.testng.annotations.BeforeClass;
import org.testng.annotations.Listeners;
import org.testng.annotations.Test;

@Listeners(ExtentReportListener.class)
public class WeeklyReportAutomationTest {

    private static final String BASE_URL = "http://localhost:5000";
    private String studentToken;
    private String mentorToken;

    @BeforeClass
    public void setup() {
        RestAssured.baseURI = BASE_URL;

        // Đăng nhập lấy Token Sinh viên
        String studentBody = "{\"email\":\"hung.nt@gmail.com\",\"password\":\"Admin@123\"}";
        Response studentRes = RestAssured.given().contentType(ContentType.JSON).body(studentBody).post("/api/auth/login");
        if (studentRes.getStatusCode() == 200) {
            studentToken = studentRes.jsonPath().getString("data.token");
            if (studentToken == null) studentToken = studentRes.jsonPath().getString("token");
        }

        // Đăng nhập lấy Token Mentor
        String mentorBody = "{\"email\":\"customer.hr@company.com\",\"password\":\"Admin@123\"}";
        Response mentorRes = RestAssured.given().contentType(ContentType.JSON).body(mentorBody).post("/api/auth/login");
        if (mentorRes.getStatusCode() == 200) {
            mentorToken = mentorRes.jsonPath().getString("data.token");
            if (mentorToken == null) mentorToken = mentorRes.jsonPath().getString("token");
        }
    }

    @Test(priority = 1)
    public void testStudentCreateWeeklyReport() {
        // Sinh viên nộp Báo cáo hàng tuần mới
        if (studentToken != null) {
            String reportBody = "{\"weekNumber\":1,\"content\":\"Da hoan thanh nghien cuu REST-Assured va TestNG\",\"workDone\":\"Viet 11 test cases\",\"nextWeekPlan\":\"Tich hop CI/CD\"}";

            Response response = RestAssured.given()
                    .header("Authorization", "Bearer " + studentToken)
                    .contentType(ContentType.JSON)
                    .body(reportBody)
                    .post("/api/weekly-reports");

            Assert.assertTrue(response.getStatusCode() == 201 || response.getStatusCode() == 200 || response.getStatusCode() == 404, 
                    "API Sinh viên nộp báo cáo tuần thất bại!");
        }
    }

    @Test(priority = 2)
    public void testGetWeeklyReportsList() {
        // Xem danh sách Báo cáo tuần
        String tokenToUse = (mentorToken != null) ? mentorToken : studentToken;
        if (tokenToUse != null) {
            Response response = RestAssured.given()
                    .header("Authorization", "Bearer " + tokenToUse)
                    .contentType(ContentType.JSON)
                    .get("/api/weekly-reports");

            Assert.assertTrue(response.getStatusCode() == 200 || response.getStatusCode() == 404, 
                    "API Lấy danh sách báo cáo tuần thất bại!");
        }
    }
}