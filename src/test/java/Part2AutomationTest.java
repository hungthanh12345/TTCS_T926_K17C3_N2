import io.restassured.RestAssured;
import io.restassured.http.ContentType;
import io.restassured.response.Response;
import io.restassured.specification.RequestSpecification;

import org.testng.Assert;
import org.testng.annotations.BeforeClass;
import org.testng.annotations.Listeners;
import org.testng.annotations.Test;

@Listeners(ExtentReportListener.class)
public class Part2AutomationTest {

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
        Assert.assertEquals(response.getStatusCode(), 200, "API Login thất bại!");

        adminToken = response.jsonPath().getString("data.token");
        if (adminToken == null) adminToken = response.jsonPath().getString("token");
    }

    @Test(priority = 2)
    public void testCreateMentorByHR() {
        String uniqueId = String.valueOf(System.currentTimeMillis() % 100000);

        String mentorBody = String.format(
            "{" +
            "\"fullName\":\"Mentor Test %s\"," +
            "\"email\":\"mentor_%s@gmail.com\"," +
            "\"phoneNumber\":\"0912345678\"," +
            "\"department\":\"IT Department\"," +
            "\"specialization\":\"Software Engineering\"" +
            "}", uniqueId, uniqueId
        );

        RequestSpecification request = RestAssured.given().contentType(ContentType.JSON);
        if (adminToken != null && !adminToken.isEmpty()) {
            request.header("Authorization", "Bearer " + adminToken);
        }

        Response response = request.body(mentorBody).post("/api/hr/mentors");

        System.out.println("=== CREATE MENTOR RESPONSE ===");
        System.out.println("Status Code: " + response.getStatusCode());
        Assert.assertTrue(response.getStatusCode() == 200 || response.getStatusCode() == 201 || response.getStatusCode() == 204, 
        "BUG DETECTED - API AssignMentor không khả dụng! Status Code: " + response.getStatusCode() + " | Response Body: " + response.asString());
    }

    @Test(priority = 3, dependsOnMethods = {"testCreateMentorByHR"})
    public void testAssignMentorToStudent() {
        String assignBody = "{\"mentorId\":1}";

         RequestSpecification request = RestAssured.given().contentType(ContentType.JSON);
        if (adminToken != null && !adminToken.isEmpty()) {
            request.header("Authorization", "Bearer " + adminToken);
        }

        Response response = request.body(assignBody).put("/api/hr/students/1/assign-mentor");

        System.out.println("=== ASSIGN MENTOR RESPONSE ===");
        System.out.println("Status Code: " + response.getStatusCode());
        System.out.println("Response Body: " + response.asString());

        // Nếu muốn hiển thị lỗi màu ĐỎ lên báo cáo ExtentReport để chụp ảnh gửi Dev:
        if (response.getStatusCode() == 405) {
            Assert.fail("BUG 405 DETECTED: Endpoint không hỗ trợ HTTP Verb! Response Body: " + response.asString());
        }

        Assert.assertEquals(response.getStatusCode(), 200, "API AssignMentor không trả về Status 200 OK!");
    }
}
