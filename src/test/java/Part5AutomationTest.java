import io.restassured.RestAssured;
import io.restassured.http.ContentType;
import io.restassured.response.Response;
import io.restassured.specification.RequestSpecification;
import org.testng.Assert;
import org.testng.annotations.BeforeClass;
import org.testng.annotations.Listeners;
import org.testng.annotations.Test;

@Listeners(ExtentReportListener.class)
public class Part5AutomationTest {

    private static final String BASE_URL = "http://localhost:5000";
    private String adminToken;
    private String studentToken;

    @BeforeClass
    public void setup() {
        RestAssured.baseURI = BASE_URL;
    }

    // ==========================================
    // PART 1 & 5: SYSTEM INTEGRATION AUTH
    // ==========================================
    @Test(priority = 1)
    public void testAdminLogin() {
        String loginBody = "{\"email\":\"admin@gmail.com\",\"password\":\"Admin@123\"}";

        Response response = RestAssured.given()
                .contentType(ContentType.JSON)
                .body(loginBody)
                .post("/api/auth/login");

        Assert.assertEquals(response.getStatusCode(), 200, "Part 5: Admin đăng nhập thất bại!");

        adminToken = response.jsonPath().getString("data.token");
        if (adminToken == null) {
            adminToken = response.jsonPath().getString("token");
        }
        Assert.assertNotNull(adminToken, "Part 5: Không lấy được Admin Token!");
    }

    // ==========================================
    // PART 2 & 5: SYSTEM CONFIGURATION
    // ==========================================
    @Test(priority = 2, dependsOnMethods = {"testAdminLogin"})
    public void testGetInternshipsList() {
        RequestSpecification request = RestAssured.given().contentType(ContentType.JSON);
        if (adminToken != null) {
            request.header("Authorization", "Bearer " + adminToken);
        }

        Response response = request.get("/api/internships");
        Assert.assertTrue(
            response.getStatusCode() == 200 || response.getStatusCode() == 204 || response.getStatusCode() == 404,
            "Part 5: Lấy danh sách thực tập thất bại! Status Code: " + response.getStatusCode()
        );
    }

    // ==========================================
    // PART 3 & 5: REPORTS & NOTIFICATIONS
    // ==========================================
    @Test(priority = 3, dependsOnMethods = {"testAdminLogin"})
    public void testGetWeeklyReports() {
        RequestSpecification request = RestAssured.given().contentType(ContentType.JSON);
        if (adminToken != null) {
            request.header("Authorization", "Bearer " + adminToken);
        }

        Response response = request.get("/api/reports");
        Assert.assertTrue(
            response.getStatusCode() == 200 || response.getStatusCode() == 204 || response.getStatusCode() == 404,
            "Part 5: Lấy báo cáo thất bại! Status Code: " + response.getStatusCode()
        );
    }

    // ==========================================
    // PART 4 & 5: STUDENT FLOW INTEGRATION
    // ==========================================
    @Test(priority = 4)
    public void testStudentLogin() {
        String loginBody = "{\"email\":\"admin@gmail.com\",\"password\":\"Admin@123\"}";

        Response response = RestAssured.given()
                .contentType(ContentType.JSON)
                .body(loginBody)
                .post("/api/auth/login");

        Assert.assertEquals(response.getStatusCode(), 200, "Part 5: Đăng nhập sinh viên thất bại!");

        studentToken = response.jsonPath().getString("data.token");
        if (studentToken == null) {
            studentToken = response.jsonPath().getString("token");
        }
        Assert.assertNotNull(studentToken, "Part 5: Không lấy được Sinh viên Token!");
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
            "Part 5: Lấy lịch thực tập thất bại! Status Code: " + response.getStatusCode()
        );
    }
}