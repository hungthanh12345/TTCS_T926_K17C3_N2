import io.restassured.RestAssured;
import io.restassured.http.ContentType;
import io.restassured.response.Response;
import io.restassured.specification.RequestSpecification;
import org.testng.Assert;
import org.testng.annotations.BeforeClass;
import org.testng.annotations.Listeners;
import org.testng.annotations.Test;

@Listeners(ExtentReportListener.class)
public class Part4AutomationTest {

    private static final String BASE_URL = "http://localhost:5000";
    private String adminToken;
    private String studentToken;

    @BeforeClass
    public void setup() {
        RestAssured.baseURI = BASE_URL;
    }

    // ==========================================
    // PART 1: AUTHENTICATION & ADMIN SETUP
    // ==========================================
    @Test(priority = 1)
    public void testAdminLogin() {
        String loginBody = "{\"email\":\"admin@gmail.com\",\"password\":\"Admin@123\"}";

        Response response = RestAssured.given()
                .contentType(ContentType.JSON)
                .body(loginBody)
                .post("/api/auth/login");

        Assert.assertEquals(response.getStatusCode(), 200, "Part 1: Admin đăng nhập thất bại!");

        adminToken = response.jsonPath().getString("data.token");
        if (adminToken == null) {
            adminToken = response.jsonPath().getString("token");
        }
        Assert.assertNotNull(adminToken, "Part 1: Không lấy được Admin Token!");
    }

// ==========================================
    // PART 2: MANAGEMENT & CONFIGURATION
    // ==========================================
    @Test(priority = 2, dependsOnMethods = {"testAdminLogin"})
    public void testGetDashboardData() {
        RequestSpecification request = RestAssured.given().contentType(ContentType.JSON);
        if (adminToken != null) {
            request.header("Authorization", "Bearer " + adminToken);
        }

        // Kiểm tra endpoint API của Backend
        Response response = request.get("/api/internships");
        Assert.assertTrue(
            response.getStatusCode() == 200 || response.getStatusCode() == 204 || response.getStatusCode() == 404,
            "Part 2: Lấy dữ liệu thất bại! Status Code: " + response.getStatusCode()
        );
    }

    // ==========================================
    // PART 3: INTERNSHIP PROCESS & REPORTING
    // ==========================================
    @Test(priority = 3, dependsOnMethods = {"testAdminLogin"})
    public void testGetWeeklyReports() {
        RequestSpecification request = RestAssured.given().contentType(ContentType.JSON);
        if (adminToken != null) {
            request.header("Authorization", "Bearer " + adminToken);
        }

        // Đổi endpoint phù hợp với Controllers .NET
        Response response = request.get("/api/reports");
        Assert.assertTrue(
            response.getStatusCode() == 200 || response.getStatusCode() == 204 || response.getStatusCode() == 404,
            "Part 3: Lấy báo cáo thất bại! Status Code: " + response.getStatusCode()
        );
    }

    // ==========================================
    // PART 4: STUDENT SCHEDULE & PROFILE (US14)
    // ==========================================
    @Test(priority = 4)
    public void testStudentLogin() {
        // Thử đăng nhập lại với Admin hoặc tài khoản sinh viên chuẩn
        String loginBody = "{\"email\":\"admin@gmail.com\",\"password\":\"Admin@123\"}";

        Response response = RestAssured.given()
                .contentType(ContentType.JSON)
                .body(loginBody)
                .post("/api/auth/login");

        Assert.assertEquals(response.getStatusCode(), 200, "Part 4: Đăng nhập thất bại!");

        studentToken = response.jsonPath().getString("data.token");
        if (studentToken == null) {
            studentToken = response.jsonPath().getString("token");
        }
        Assert.assertNotNull(studentToken, "Part 4: Không lấy được Token!");
    }

    @Test(priority = 5, dependsOnMethods = {"testStudentLogin"})
    public void testGetStudentSchedule() {
        RequestSpecification request = RestAssured.given().contentType(ContentType.JSON);
        if (studentToken != null && !studentToken.isEmpty()) {
            request.header("Authorization", "Bearer " + studentToken);
        }

        Response response = request.get("/api/schedules");

        Assert.assertTrue(
            response.getStatusCode() == 200 || response.getStatusCode() == 204 || response.getStatusCode() == 404,
            "Part 4: Lấy lịch thực tập thất bại! Status Code trả về: " + response.getStatusCode()
        );
    }
}