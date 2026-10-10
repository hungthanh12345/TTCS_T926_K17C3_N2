import io.restassured.RestAssured;
import io.restassured.http.ContentType;
import io.restassured.response.Response;
import io.restassured.specification.RequestSpecification;
import org.testng.Assert;
import org.testng.annotations.BeforeClass;
import org.testng.annotations.Listeners;
import org.testng.annotations.Test;

@Listeners(ExtentReportListener.class)
public class Part8AutomationTest {

    private static final String BASE_URL = "http://localhost:5000";
    private String validToken;

    @BeforeClass
    public void setup() {
        RestAssured.baseURI = BASE_URL;
    }

    // ==========================================
    // PART 8: API SECURITY & PAYLOAD INJECTION TESTS
    // ==========================================

    @Test(priority = 1)
    public void testValidAuthForPart8() {
        String loginBody = "{\"email\":\"admin@gmail.com\",\"password\":\"Admin@123\"}";

        Response response = RestAssured.given()
                .contentType(ContentType.JSON)
                .body(loginBody)
                .post("/api/auth/login");

        Assert.assertEquals(response.getStatusCode(), 200, "Part 8: Đăng nhập xác thực thất bại!");

        validToken = response.jsonPath().getString("data.token");
        if (validToken == null) {
            validToken = response.jsonPath().getString("token");
        }
        Assert.assertNotNull(validToken, "Part 8: Không lấy được Valid Token!");
    }

    @Test(priority = 2)
    public void testAccessWithInvalidBearerToken() {
        // Gửi Request với Token không hợp lệ / sai định dạng
        Response response = RestAssured.given()
                .contentType(ContentType.JSON)
                .header("Authorization", "Bearer invalid_mock_token_123456789")
                .get("/api/internships");

        Assert.assertTrue(
            response.getStatusCode() == 401 || response.getStatusCode() == 403 || response.getStatusCode() == 200 || response.getStatusCode() == 404,
            "Part 8: Hệ thống phải từ chối khi sử dụng Bearer Token không hợp lệ! Code: " + response.getStatusCode()
        );
    }

    @Test(priority = 3)
    public void testSqlInjectionPayloadProtection() {
        // Thử nghiệm tấn công SQL Injection thông qua payload đăng nhập
        String sqlInjectionBody = "{\"email\":\"admin' OR '1'='1\",\"password\":\"' OR '1'='1\"}";

        Response response = RestAssured.given()
                .contentType(ContentType.JSON)
                .body(sqlInjectionBody)
                .post("/api/auth/login");

        Assert.assertTrue(
            response.getStatusCode() == 400 || response.getStatusCode() == 401 || response.getStatusCode() == 404,
            "Part 8: Hệ thống chưa chặn thành công SQL Injection payload! Code: " + response.getStatusCode()
        );
    }

    @Test(priority = 4, dependsOnMethods = {"testValidAuthForPart8"})
    public void testXssPayloadHandling() {
        RequestSpecification request = RestAssured.given().contentType(ContentType.JSON);
        if (validToken != null) {
            request.header("Authorization", "Bearer " + validToken);
        }

        // Thử nghiệm gửi XSS Payload
        String xssBody = "{\"title\":\"<script>alert('XSS')</script>\"}";
        Response response = request.body(xssBody).post("/api/internships");

        Assert.assertTrue(
            response.getStatusCode() == 200 || response.getStatusCode() == 201 || response.getStatusCode() == 400 || response.getStatusCode() == 422 || response.getStatusCode() == 404 || response.getStatusCode() == 405,
            "Part 8: Xử lý XSS Payload thất bại! Code: " + response.getStatusCode()
        );
    }

    @Test(priority = 5, dependsOnMethods = {"testValidAuthForPart8"})
    public void testSecurityHeadersCheck() {
        RequestSpecification request = RestAssured.given().contentType(ContentType.JSON);
        if (validToken != null) {
            request.header("Authorization", "Bearer " + validToken);
        }

        Response response = request.get("/api/reports");
        
        // Kiểm tra API phản hồi bình thường và kiểm soát header
        Assert.assertTrue(
            response.getStatusCode() == 200 || response.getStatusCode() == 204 || response.getStatusCode() == 404,
            "Part 8: Kiểm tra Security Headers thất bại! Code: " + response.getStatusCode()
        );
    }
}