import io.restassured.RestAssured;
import io.restassured.http.ContentType;
import io.restassured.response.Response;
import org.testng.Assert;
import org.testng.annotations.BeforeClass;
import org.testng.annotations.Listeners;
import org.testng.annotations.Test;

@Listeners(ExtentReportListener.class)
public class Sprint1AutomationTest {

    private static final String BASE_URL = "http://localhost:5000";
    private String adminToken;
    private String hrToken;
    private String studentToken;

    @BeforeClass
    public void setup() {
        RestAssured.baseURI = BASE_URL;

        // 1. Đăng nhập Admin
        String adminBody = "{\"email\":\"admin@gmail.com\",\"password\":\"Admin@123\"}";
        Response adminRes = RestAssured.given().contentType(ContentType.JSON).body(adminBody).post("/api/auth/login");
        Assert.assertEquals(adminRes.getStatusCode(), 200, "Đăng nhập Admin thất bại!");
        adminToken = getTokenFromResponse(adminRes);

        // 2. Đăng nhập HR
        String hrBody = "{\"email\":\"customer.hr@company.com\",\"password\":\"Admin@123\"}";
        Response hrRes = RestAssured.given().contentType(ContentType.JSON).body(hrBody).post("/api/auth/login");
        if (hrRes.getStatusCode() == 200) {
            hrToken = getTokenFromResponse(hrRes);
        }

        // 3. Đăng nhập Student
        String studentBody = "{\"email\":\"hung.nt@gmail.com\",\"password\":\"Admin@123\"}";
        Response studentRes = RestAssured.given().contentType(ContentType.JSON).body(studentBody).post("/api/auth/login");
        if (studentRes.getStatusCode() == 200) {
            studentToken = getTokenFromResponse(studentRes);
        }
    }

    private String getTokenFromResponse(Response response) {
        String token = response.jsonPath().getString("data.token");
        return (token != null) ? token : response.jsonPath().getString("token");
    }

    // =========================================================================
    // US40: ĐĂNG NHẬP, XÁC THỰC MẬT KHẨU & CHỐNG TRUY CẬP TRÁI PHÉP
    // =========================================================================

    @Test(priority = 1)
    public void testLoginWithWrongPassword() {
        String wrongBody = "{\"email\":\"admin@gmail.com\",\"password\":\"WrongPassword123\"}";
        Response response = RestAssured.given().contentType(ContentType.JSON).body(wrongBody).post("/api/auth/login");
        Assert.assertEquals(response.getStatusCode(), 401, "Server phải trả về 401 khi sai mật khẩu!");
    }

    @Test(priority = 2)
    public void testStudentCannotAccessAdminAPI() {
        if (studentToken != null) {
            Response response = RestAssured.given()
                    .header("Authorization", "Bearer " + studentToken)
                    .contentType(ContentType.JSON)
                    .get("/api/admin/users");

            Assert.assertEquals(response.getStatusCode(), 403, "Sinh viên không được phép truy cập API của Admin!");
        }
    }

    // =========================================================================
    // US39: ADMIN QUẢN LÝ VÀ TẠO TÀI KHOẢN
    // =========================================================================

    @Test(priority = 3)
    public void testAdminGetUsers() {
        Response response = RestAssured.given()
                .header("Authorization", "Bearer " + adminToken)
                .contentType(ContentType.JSON)
                .get("/api/admin/users");

        Assert.assertEquals(response.getStatusCode(), 200, "Admin không lấy được danh sách User!");
    }

    @Test(priority = 4)
    public void testAdminCreateUser() {
        long timestamp = System.currentTimeMillis();
        String newUserBody = String.format("{\"email\":\"test.mentor%d@gmail.com\",\"password\":\"Admin@123\",\"roleName\":\"ROLE_MENTOR\",\"status\":0}", timestamp);

        Response response = RestAssured.given()
                .header("Authorization", "Bearer " + adminToken)
                .contentType(ContentType.JSON)
                .body(newUserBody)
                .post("/api/admin/users");

        Assert.assertTrue(response.getStatusCode() == 201 || response.getStatusCode() == 200, "Admin tạo User mới thất bại!");
    }

    // =========================================================================
    // US01 & US03: HR THÊM MỚI, TÌM KIẾM, LỌC & PHÂN TRANG THỰC TẬP SINH
    // =========================================================================

    @Test(priority = 5)
    public void testHRCreateStudent() {
        String tokenToUse = (hrToken != null) ? hrToken : adminToken;
        long timestamp = System.currentTimeMillis();
        String studentBody = String.format("{\"studentCode\":\"SV%d\",\"fullName\":\"Auto Test Student\",\"phoneNumber\":\"0988888888\",\"university\":\"ICTU\",\"major\":\"Information Technology\"}", timestamp);

        Response response = RestAssured.given()
                .header("Authorization", "Bearer " + tokenToUse)
                .contentType(ContentType.JSON)
                .body(studentBody)
                .post("/api/hr/students");

        Assert.assertTrue(response.getStatusCode() == 201 || response.getStatusCode() == 200, "HR tạo hồ sơ Sinh viên mới thất bại!");
    }

    @Test(priority = 6)
    public void testHRGetStudentsSearch() {
        String tokenToUse = (hrToken != null) ? hrToken : adminToken;
        Response response = RestAssured.given()
                .header("Authorization", "Bearer " + tokenToUse)
                .contentType(ContentType.JSON)
                .get("/api/hr/students/search?page=1&pageSize=10");

        Assert.assertEquals(response.getStatusCode(), 200, "HR gọi API search sinh viên thất bại!");
    }

    // =========================================================================
    // US02: HR CHỈNH SỬA HỒ SƠ THỰC TẬP SINH
    // =========================================================================

    @Test(priority = 7)
    public void testHRUpdateStudentProfile() {
        String tokenToUse = (hrToken != null) ? hrToken : adminToken;
        String updateBody = "{\"fullName\":\"Student Updated Name\",\"phoneNumber\":\"0911223344\",\"university\":\"ICTU\",\"major\":\"Software Engineering\"}";

        Response response = RestAssured.given()
                .header("Authorization", "Bearer " + tokenToUse)
                .contentType(ContentType.JSON)
                .body(updateBody)
                .put("/api/hr/students/1");

        Assert.assertTrue(response.getStatusCode() == 200 || response.getStatusCode() == 404, "Cập nhật hồ sơ thực tập sinh thất bại!");
    }

    // =========================================================================
    // US29 & US30: HR QUẢN LÝ MENTOR & PHÂN CÔNG MENTOR
    // =========================================================================

    @Test(priority = 8)
    public void testHRGetMentors() {
        String tokenToUse = (hrToken != null) ? hrToken : adminToken;
        Response response = RestAssured.given()
                .header("Authorization", "Bearer " + tokenToUse)
                .contentType(ContentType.JSON)
                .get("/api/hr/mentors");

        Assert.assertEquals(response.getStatusCode(), 200, "HR xem danh sách Mentor thất bại!");
    }

    @Test(priority = 9)
    public void testHRAssignMentorToStudent() {
        String tokenToUse = (hrToken != null) ? hrToken : adminToken;
        String assignBody = "{\"mentorId\":1}";

        Response response = RestAssured.given()
                .header("Authorization", "Bearer " + tokenToUse)
                .contentType(ContentType.JSON)
                .body(assignBody)
                .put("/api/hr/students/1/assign-mentor");

        Assert.assertTrue(response.getStatusCode() == 200 || response.getStatusCode() == 404, "Gán Mentor cho thực tập sinh thất bại!");
    }
}