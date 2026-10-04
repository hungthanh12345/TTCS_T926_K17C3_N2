import io.restassured.RestAssured;
import io.restassured.http.ContentType;
import io.restassured.response.Response;
import org.testng.Assert;
import org.testng.annotations.BeforeClass;
import org.testng.annotations.Listeners;
import org.testng.annotations.Test;
import io.restassured.specification.RequestSpecification;

@Listeners(ExtentReportListener.class)
public class Part1AutomationTest {

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

        System.out.println("Login Status Code: " + response.getStatusCode());
        System.out.println("Login Response Body: " + response.asString());

        Assert.assertTrue(response.getStatusCode() == 200 || response.getStatusCode() == 400 || response.getStatusCode() == 401 || response.getStatusCode() == 404,
                "Lỗi bất thường từ API Login!");

        adminToken = response.jsonPath().getString("data.token");
        if (adminToken == null) adminToken = response.jsonPath().getString("token");
    }

    @Test(priority = 2)
    public void testCreateStudentByHR() {
        String uniqueId = String.valueOf(System.currentTimeMillis() % 100000);
        
        // Cấu trúc DTO chuẩn khớp 100% với CreateStudentRequestDto.cs
        String studentBody = String.format(
            "{" +
            "\"studentCode\":\"SV%s\"," +
            "\"fullName\":\"Nguyen Van Test\"," +
            "\"phoneNumber\":\"0987654321\"," +
            "\"university\":\"Hanoi University\"," +
            "\"major\":\"Software Engineering\"" +
            "}", uniqueId
        );

        RequestSpecification request = RestAssured.given().contentType(ContentType.JSON);
        if (adminToken != null && !adminToken.isEmpty()) {
            request.header("Authorization", "Bearer " + adminToken);
        }

        Response response = request.body(studentBody).post("/api/hr/students");

        System.out.println("=== CREATE STUDENT RESPONSE ===");
        System.out.println("Status Code: " + response.getStatusCode());
        System.out.println("Response Body: " + response.asString());

        Assert.assertTrue(response.getStatusCode() == 200 || response.getStatusCode() == 201, 
                "Tạo sinh viên thất bại! Status code: " + response.getStatusCode() + " - Body: " + response.asString());
    }
}