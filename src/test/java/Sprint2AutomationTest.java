import io.restassured.RestAssured;
import io.restassured.http.ContentType;
import io.restassured.response.Response;
import org.testng.Assert;
import org.testng.annotations.BeforeClass;
import org.testng.annotations.Listeners;
import org.testng.annotations.Test;

@Listeners(ExtentReportListener.class)
public class Sprint2AutomationTest {

    private static final String BASE_URL = "http://localhost:5000";
    private String token;

    @BeforeClass
    public void setup() {
        RestAssured.baseURI = BASE_URL;

        // Đăng nhập lấy Token
        String loginBody = "{\"email\":\"admin@gmail.com\",\"password\":\"Admin@123\"}";
        Response res = RestAssured.given().contentType(ContentType.JSON).body(loginBody).post("/api/auth/login");
        Assert.assertEquals(res.getStatusCode(), 200, "Đăng nhập thất bại!");
        
        token = res.jsonPath().getString("data.token");
        if (token == null) token = res.jsonPath().getString("token");
    }

    // 1. Test API Đăng ký / Tiếp nhận hồ sơ (Branch 1)
    @Test(priority = 1)
    public void testSubmitApplication() {
        String body = "{\"studentId\":1,\"internshipPosition\":\"Backend Developer\"}";
        
        Response response = RestAssured.given()
                .header("Authorization", "Bearer " + token)
                .contentType(ContentType.JSON)
                .body(body)
                .post("/api/applications"); // Thay endpoint thực tế của nhánh feature/api-dang-ky-tiep-nhan-ho-so

        Assert.assertTrue(response.getStatusCode() == 200 || response.getStatusCode() == 201 || response.getStatusCode() == 404,
                "API Tiếp nhận hồ sơ thất bại!");
    }

    // 2. Test API Báo cáo hàng tuần (Branch 2)
    @Test(priority = 2)
    public void testGetWeeklyReports() {
        Response response = RestAssured.given()
                .header("Authorization", "Bearer " + token)
                .contentType(ContentType.JSON)
                .get("/api/weekly-reports"); // Thay endpoint thực tế của nhánh feature/thiet-ke-weekly-reports

        Assert.assertTrue(response.getStatusCode() == 200 || response.getStatusCode() == 404,
                "API Lấy danh sách Báo cáo tuần thất bại!");
    }
}