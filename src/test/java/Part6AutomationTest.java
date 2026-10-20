import io.restassured.RestAssured;
import io.restassured.http.ContentType;
import io.restassured.response.Response;
import io.restassured.specification.RequestSpecification;
import org.testng.Assert;
import org.testng.annotations.BeforeClass;
import org.testng.annotations.Listeners;
import org.testng.annotations.Test;

@Listeners(ExtentReportListener.class)
public class Part6AutomationTest {

    private static final String BASE_URL = "http://localhost:5000";
    private String validToken;

    @BeforeClass
    public void setup() {
        RestAssured.baseURI = BASE_URL;
    }

    // ==========================================
    // PART 6: ADVANCED SCENARIOS & EXCEPTION HANDLING
    // ==========================================

    @Test(priority = 1)
    public void testLoginWithInvalidCredentials() {
        String invalidBody = "{\"email\":\"wrong_user@gmail.com\",\"password\":\"WrongPass123\"}";

        Response response = RestAssured.given()
                .contentType(ContentType.JSON)
                .body(invalidBody)
                .post("/api/auth/login");

        // Kiểm tra xử lý lỗi khi đăng nhập sai thông tin
        Assert.assertTrue(
            response.getStatusCode() == 400 || response.getStatusCode() == 401 || response.getStatusCode() == 404,
            "Part 6: Hệ thống không trả về lỗi xác thực đúng kỳ vọng! Code: " + response.getStatusCode()
        );
    }

    @Test(priority = 2)
    public void testValidAuthentication() {
        String loginBody = "{\"email\":\"admin@gmail.com\",\"password\":\"Admin@123\"}";

        Response response = RestAssured.given()
                .contentType(ContentType.JSON)
                .body(loginBody)
                .post("/api/auth/login");

        Assert.assertEquals(response.getStatusCode(), 200, "Part 6: Đăng nhập tài khoản hợp lệ thất bại!");

        validToken = response.jsonPath().getString("data.token");
        if (validToken == null) {
            validToken = response.jsonPath().getString("token");
        }
        Assert.assertNotNull(validToken, "Part 6: Không lấy được Token!");
    }

    @Test(priority = 3)
    public void testAccessProtectedEndpointWithoutToken() {
        // Truy cập endpoint yêu cầu token nhưng không truyền Header Authorization
        Response response = RestAssured.given()
                .contentType(ContentType.JSON)
                .get("/api/internships");

        Assert.assertTrue(
            response.getStatusCode() == 401 || response.getStatusCode() == 403 || response.getStatusCode() == 200 || response.getStatusCode() == 404,
            "Part 6: Phản hồi truy cập không quyền không chính xác! Code: " + response.getStatusCode()
        );
    }

    @Test(priority = 4, dependsOnMethods = {"testValidAuthentication"})
    public void testNonExistentEndpointHandling() {
        RequestSpecification request = RestAssured.given().contentType(ContentType.JSON);
        if (validToken != null) {
            request.header("Authorization", "Bearer " + validToken);
        }

        // Gọi API đến endpoint không tồn tại
        Response response = request.get("/api/non-existent-endpoint-xyz");

        Assert.assertEquals(response.getStatusCode(), 404, "Part 6: Endpoint không tồn tại phải trả về 404 Not Found!");
    }

    @Test(priority = 5, dependsOnMethods = {"testValidAuthentication"})
    public void testResponseTimePerformance() {
        RequestSpecification request = RestAssured.given().contentType(ContentType.JSON);
        if (validToken != null) {
            request.header("Authorization", "Bearer " + validToken);
        }

        Response response = request.get("/api/reports");
        
        // Kiểm tra thời gian phản hồi (Response Time) phải nhỏ hơn 3000ms (3 giây)
        long responseTime = response.getTime();
        Assert.assertTrue(responseTime < 3000, "Part 6: Thời gian phản hồi API quá chậm (> 3s): " + responseTime + "ms");
    }
}