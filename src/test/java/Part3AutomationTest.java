import io.restassured.RestAssured;
import io.restassured.http.ContentType;
import io.restassured.response.Response;
import io.restassured.specification.RequestSpecification;
import org.testng.Assert;
import org.testng.annotations.BeforeClass;
import org.testng.annotations.Listeners;
import org.testng.annotations.Test;

@Listeners(ExtentReportListener.class)
public class Part3AutomationTest {

    private static final String BASE_URL = "http://localhost:5000";
    private String adminToken;

    @BeforeClass
    public void setup() {
        RestAssured.baseURI = BASE_URL;
    }

    @Test(priority = 1)
    public void testAuthLogin() {
        String loginBody = "{\"email\":\"admin@gmail.com\",\"password\":\"Admin@123\"}";

        Response response = RestAssured.given()
                .contentType(ContentType.JSON)
                .body(loginBody)
                .post("/api/auth/login");

        Assert.assertEquals(response.getStatusCode(), 200, "Đăng nhập thất bại!");

        adminToken = response.jsonPath().getString("data.token");
        if (adminToken == null) adminToken = response.jsonPath().getString("token");
    }

    @Test(priority = 2, dependsOnMethods = {"testAuthLogin"})
    public void testGetHrStudents() {
        RequestSpecification request = RestAssured.given().contentType(ContentType.JSON);
        if (adminToken != null && !adminToken.isEmpty()) {
            request.header("Authorization", "Bearer " + adminToken);
        }

        // Endpoint chính xác theo HrController.cs
        Response response = request.get("/api/hr/students");

        System.out.println("=== PART 3: HR GET STUDENTS RESPONSE ===");
        System.out.println("Status Code: " + response.getStatusCode());
        System.out.println("Response Body: " + response.asString());

        Assert.assertTrue(
            response.getStatusCode() == 200 || response.getStatusCode() == 201,
            "Lấy danh sách sinh viên HR thất bại! Returned Status Code: " + response.getStatusCode()
        );
    }
}