import io.restassured.RestAssured;
import io.restassured.http.ContentType;
import io.restassured.response.Response;
import org.testng.Assert;
import org.testng.SkipException;
import org.testng.annotations.BeforeClass;
import org.testng.annotations.Listeners;
import org.testng.annotations.Test;

/**
 * US10 - Là thực tập sinh, tôi muốn xác nhận hợp đồng trên hệ thống để hoàn tất thủ tục.
 * Test xác nhận hợp đồng, quyền truy cập và trường hợp xác nhận lại.
 * Yêu cầu: backend chạy ở localhost:5000, đã hoàn thành US09 và HR đã tải hợp đồng lên cho sinh viên hung.nt@gmail.com.
 */
@Listeners(ExtentReportListener.class)
public class Us10ContractConfirmationTest {

    private static final String BASE_URL = "http://localhost:5000";
    private static final String CONTRACT_URL = "/api/student/contract";
    private static final String PASSWORD = "Admin@123";

    private String studentToken;
    private String otherStudentToken;
    private String hrToken;
    private int contractId;

    private String login(String email) {
        String body = "{\"email\":\"" + email + "\",\"password\":\"" + PASSWORD + "\"}";
        Response res = RestAssured.given().contentType(ContentType.JSON).body(body).post("/api/auth/login");
        if (res.getStatusCode() != 200) return null;
        String token = res.jsonPath().getString("data.token");
        return token != null ? token : res.jsonPath().getString("token");
    }

    private Response getContract(String token) {
        return RestAssured.given().header("Authorization", "Bearer " + token).get(CONTRACT_URL);
    }

    private Response confirm(String token, int id) {
        return RestAssured.given().header("Authorization", "Bearer " + token)
                .contentType(ContentType.JSON).post(CONTRACT_URL + "/" + id + "/confirm");
    }

    @BeforeClass
    public void setup() {
        RestAssured.baseURI = BASE_URL;
        studentToken = login("hung.nt@gmail.com");
        otherStudentToken = login("hai.nh@gmail.com");
        hrToken = login("customer.hr@company.com");
        Assert.assertNotNull(studentToken, "Không đăng nhập được tài khoản sinh viên!");
    }

    @Test(priority = 1)
    public void testStudentGetsOwnContract() {
        Response res = getContract(studentToken);
        if (res.getStatusCode() == 404) {
            throw new SkipException("Sinh viên chưa có hợp đồng - HR cần tải hợp đồng lên (US09) trước.");
        }
        Assert.assertEquals(res.getStatusCode(), 200);
        contractId = res.jsonPath().getInt("data.id");
        Assert.assertNotNull(res.jsonPath().getString("data.contractNumber"));
        Assert.assertNotNull(res.jsonPath().getString("data.status"));
    }

    @Test(priority = 2)
    public void testGetContractWithoutTokenIsUnauthorized() {
        Response res = RestAssured.given().get(CONTRACT_URL);
        Assert.assertEquals(res.getStatusCode(), 401);
    }

    @Test(priority = 3)
    public void testNonStudentRoleIsForbidden() {
        if (hrToken == null) throw new SkipException("Không đăng nhập được tài khoản HR.");
        Assert.assertEquals(getContract(hrToken).getStatusCode(), 403);
        Assert.assertEquals(confirm(hrToken, 1).getStatusCode(), 403);
    }

    @Test(priority = 4, dependsOnMethods = "testStudentGetsOwnContract")
    public void testOtherStudentCannotConfirmThisContract() {
        if (otherStudentToken == null) throw new SkipException("Không đăng nhập được sinh viên thứ hai.");
        // Hợp đồng của sinh viên khác bị coi như không tồn tại (404), không được xác nhận hộ.
        Assert.assertEquals(confirm(otherStudentToken, contractId).getStatusCode(), 404);
        Assert.assertEquals(RestAssured.given().header("Authorization", "Bearer " + otherStudentToken)
                .get(CONTRACT_URL + "/" + contractId + "/download").getStatusCode(), 404);
    }

    @Test(priority = 5)
    public void testConfirmNonExistentContract() {
        Assert.assertEquals(confirm(studentToken, 999999).getStatusCode(), 404);
    }

    @Test(priority = 6, dependsOnMethods = "testStudentGetsOwnContract")
    public void testConfirmContractSuccess() {
        String status = getContract(studentToken).jsonPath().getString("data.status");
        if (!"PENDING_CONFIRMATION".equals(status)) {
            throw new SkipException("Hợp đồng không ở trạng thái chờ xác nhận (" + status + ").");
        }
        Response res = confirm(studentToken, contractId);
        Assert.assertEquals(res.getStatusCode(), 200);
        Assert.assertEquals(res.jsonPath().getString("data.status"), "CONFIRMED");
        Assert.assertNotNull(res.jsonPath().getString("data.confirmedAt"));
    }

    @Test(priority = 7, dependsOnMethods = "testStudentGetsOwnContract")
    public void testStatusPersistedAfterConfirm() {
        Response res = getContract(studentToken);
        Assert.assertEquals(res.getStatusCode(), 200);
        Assert.assertEquals(res.jsonPath().getString("data.status"), "CONFIRMED");
        Assert.assertNotNull(res.jsonPath().getString("data.confirmedAt"));
    }

    @Test(priority = 8, dependsOnMethods = "testStudentGetsOwnContract")
    public void testConfirmAgainIsRejected() {
        String firstConfirmedAt = getContract(studentToken).jsonPath().getString("data.confirmedAt");
        Response res = confirm(studentToken, contractId);
        Assert.assertEquals(res.getStatusCode(), 409);
        // Thời điểm xác nhận ban đầu không bị ghi đè.
        Assert.assertEquals(getContract(studentToken).jsonPath().getString("data.confirmedAt"), firstConfirmedAt);
    }
}
