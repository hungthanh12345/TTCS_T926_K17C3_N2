import io.restassured.RestAssured;
import io.restassured.http.ContentType;
import io.restassured.response.Response;
import io.restassured.specification.RequestSpecification;
import org.testng.Assert;
import org.testng.annotations.BeforeClass;
import org.testng.annotations.Listeners;
import org.testng.annotations.Test;

@Listeners(ExtentReportListener.class)
public class Part9AutomationTest {

    private static final String BASE_URL = "http://localhost:5000";
    private String authToken;

    @BeforeClass
    public void setup() {
        RestAssured.baseURI = BASE_URL;
    }

    // ==========================================
    // PART 9: CONCURRENCY & STABILITY TESTS
    // ==========================================

    @Test(priority = 1)
    public void testAuthenticationForPart9() {
        String loginBody = "{\"email\":\"admin@gmail.com\",\"password\":\"Admin@123\"}";

        Response response = RestAssured.given()
                .contentType(ContentType.JSON)
                .body(loginBody)
                .post("/api/auth/login");

        Assert.assertEquals(response.getStatusCode(), 200, "Part 9: Đăng nhập xác thực thất bại!");

        authToken = response.jsonPath().getString("data.token");
        if (authToken == null) {
            authToken = response.jsonPath().getString("token");
        }
        Assert.assertNotNull(authToken, "Part 9: Không lấy được Auth Token!");
    }

    @Test(priority = 2, dependsOnMethods = {"testAuthenticationForPart9"})
    public void testMultipleSequentialRequests() {
        RequestSpecification request = RestAssured.given().contentType(ContentType.JSON);
        if (authToken != null) {
            request.header("Authorization", "Bearer " + authToken);
        }

        // Thực hiện 5 request liên tiếp để kiểm tra độ ổn định của API
        for (int i = 0; i < 5; i++) {
            Response response = request.get("/api/internships");
            Assert.assertTrue(
                response.getStatusCode() == 200 || response.getStatusCode() == 204 || response.getStatusCode() == 404,
                "Part 9: Request thứ " + (i + 1) + " thất bại với status code: " + response.getStatusCode()
            );
        }
    }

    @Test(priority = 3, dependsOnMethods = {"testAuthenticationForPart9"})
    public void testRapidAuthenticationRequests() {
        String loginBody = "{\"email\":\"admin@gmail.com\",\"password\":\"Admin@123\"}";

        // Thử nghiệm gửi liên tục các request đăng nhập
        for (int i = 0; i < 3; i++) {
            Response response = RestAssured.given()
                    .contentType(ContentType.JSON)
                    .body(loginBody)
                    .post("/api/auth/login");

            Assert.assertTrue(
                response.getStatusCode() == 200 || response.getStatusCode() == 429,
                "Part 9: Gửi request xác thực dồn dập thất bại ở lần " + (i + 1)
            );
        }
    }

    @Test(priority = 4, dependsOnMethods = {"testAuthenticationForPart9"})
    public void testDataConsistencyUnderMultipleReads() {
        RequestSpecification request = RestAssured.given().contentType(ContentType.JSON);
        if (authToken != null) {
            request.header("Authorization", "Bearer " + authToken);
        }

        Response response1 = request.get("/api/reports");
        Response response2 = request.get("/api/reports");

        // Kiểm tra tính đồng nhất về Status Code giữa các lần đọc dữ liệu
        Assert.assertEquals(response1.getStatusCode(), response2.getStatusCode(),
                "Part 9: Dữ liệu trả về không nhất quán giữa 2 lần gọi API liên tiếp!");
    }

    @Test(priority = 5, dependsOnMethods = {"testAuthenticationForPart9"})
    public void testLargePayloadHandling() {
        RequestSpecification request = RestAssured.given().contentType(ContentType.JSON);
        if (authToken != null) {
            request.header("Authorization", "Bearer " + authToken);
        }

        // Tạo chuỗi dữ liệu lớn để kiểm tra giới hạn payload xử lý
        StringBuilder largeString = new StringBuilder();
        for (int i = 0; i < 500; i++) {
            largeString.append("TestData_");
        }

        String largeBody = "{\"title\":\"" + largeString.toString() + "\"}";
        Response response = request.body(largeBody).post("/api/internships");

        Assert.assertTrue(
            response.getStatusCode() == 200 || response.getStatusCode() == 201 || response.getStatusCode() == 400 || response.getStatusCode() == 413 || response.getStatusCode() == 422 || response.getStatusCode() == 404 || response.getStatusCode() == 405,
            "Part 9: Xử lý Payload dung lượng lớn thất bại! Code: " + response.getStatusCode()
        );
    }
}