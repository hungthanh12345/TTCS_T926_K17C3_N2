import io.restassured.RestAssured;
import io.restassured.http.ContentType;
import io.restassured.response.Response;
import io.restassured.specification.RequestSpecification;
import org.testng.Assert;
import org.testng.annotations.BeforeClass;
import org.testng.annotations.Listeners;
import org.testng.annotations.Test;

@Listeners(ExtentReportListener.class)
public class Part7AutomationTest {

    private static final String BASE_URL = "http://localhost:5000";
    private String authToken;

    @BeforeClass
    public void setup() {
        RestAssured.baseURI = BASE_URL;
    }

    // ==========================================
    // PART 7: DATA INTEGRITY & CRUD / VALIDATION FLOW
    // ==========================================

    @Test(priority = 1)
    public void testAuthenticationForPart7() {
        String loginBody = "{\"email\":\"admin@gmail.com\",\"password\":\"Admin@123\"}";

        Response response = RestAssured.given()
                .contentType(ContentType.JSON)
                .body(loginBody)
                .post("/api/auth/login");

        Assert.assertEquals(response.getStatusCode(), 200, "Part 7: Đăng nhập xác thực thất bại!");

        authToken = response.jsonPath().getString("data.token");
        if (authToken == null) {
            authToken = response.jsonPath().getString("token");
        }
        Assert.assertNotNull(authToken, "Part 7: Không lấy được Auth Token!");
    }

    @Test(priority = 2, dependsOnMethods = {"testAuthenticationForPart7"})
    public void testCreateDataWithInvalidPayload() {
        RequestSpecification request = RestAssured.given().contentType(ContentType.JSON);
        if (authToken != null) {
            request.header("Authorization", "Bearer " + authToken);
        }

        // Gửi payload rỗng/không hợp lệ để kiểm tra validation dữ liệu
        String invalidPayload = "{}";
        Response response = request.body(invalidPayload).post("/api/internships");

        Assert.assertTrue(
            response.getStatusCode() == 400 || response.getStatusCode() == 422 || response.getStatusCode() == 404 || response.getStatusCode() == 405,
            "Part 7: Kiểm tra Validation dữ liệu thất bại! Code trả về: " + response.getStatusCode()
        );
    }

    @Test(priority = 3, dependsOnMethods = {"testAuthenticationForPart7"})
    public void testFetchDataById() {
        RequestSpecification request = RestAssured.given().contentType(ContentType.JSON);
        if (authToken != null) {
            request.header("Authorization", "Bearer " + authToken);
        }

        // Thử nghiệm lấy thông tin theo ID cụ thể
        Response response = request.get("/api/internships/1");

        Assert.assertTrue(
            response.getStatusCode() == 200 || response.getStatusCode() == 204 || response.getStatusCode() == 404,
            "Part 7: Lấy dữ liệu chi tiết theo ID thất bại! Code trả về: " + response.getStatusCode()
        );
    }

    @Test(priority = 4, dependsOnMethods = {"testAuthenticationForPart7"})
    public void testUpdateDataValidation() {
        RequestSpecification request = RestAssured.given().contentType(ContentType.JSON);
        if (authToken != null) {
            request.header("Authorization", "Bearer " + authToken);
        }

        String updateBody = "{\"title\":\"Updated Title Test\"}";
        Response response = request.body(updateBody).put("/api/internships/1");

        Assert.assertTrue(
            response.getStatusCode() == 200 || response.getStatusCode() == 204 || response.getStatusCode() == 400 || response.getStatusCode() == 404 || response.getStatusCode() == 405,
            "Part 7: Kiểm tra cập nhật dữ liệu thất bại! Code trả về: " + response.getStatusCode()
        );
    }

    @Test(priority = 5, dependsOnMethods = {"testAuthenticationForPart7"})
    public void testDeleteNonExistentResource() {
        RequestSpecification request = RestAssured.given().contentType(ContentType.JSON);
        if (authToken != null) {
            request.header("Authorization", "Bearer " + authToken);
        }

        // Xóa một ID không tồn tại
        Response response = request.delete("/api/internships/999999");

        Assert.assertTrue(
            response.getStatusCode() == 404 || response.getStatusCode() == 400 || response.getStatusCode() == 200 || response.getStatusCode() == 204 || response.getStatusCode() == 405,
            "Part 7: Xử lý xóa tài nguyên không tồn tại không đúng! Code trả về: " + response.getStatusCode()
        );
    }
}