import io.restassured.RestAssured;
import io.restassured.http.ContentType;
import io.restassured.response.Response;
import org.testng.Assert;
import org.testng.SkipException;
import org.testng.annotations.BeforeClass;
import org.testng.annotations.Listeners;
import org.testng.annotations.Test;

import java.nio.charset.StandardCharsets;
import java.util.List;
import java.util.Map;

/**
 * US09 - Là HR, tôi muốn tải lên hợp đồng thực tập để quản lý giấy tờ.
 * Test upload file hợp lệ, sai định dạng, quá dung lượng và file trùng.
 * Yêu cầu: backend chạy ở localhost:5000, đã chạy migration 20261010_us09_internship_contracts.sql.
 * Test chọn một sinh viên chưa xác nhận hợp đồng; không đụng tới hợp đồng đã xác nhận.
 */
@Listeners(ExtentReportListener.class)
public class Us09ContractUploadTest {

    private static final String BASE_URL = "http://localhost:5000";
    private static final String HR_CONTRACTS = "/api/hr/contracts";
    private static final String PASSWORD = "Admin@123";
    private static final int MAX_BYTES = 10 * 1024 * 1024;

    private String hrToken;
    private String studentToken;
    private int studentId;

    private String login(String email) {
        String body = "{\"email\":\"" + email + "\",\"password\":\"" + PASSWORD + "\"}";
        Response res = RestAssured.given().contentType(ContentType.JSON).body(body).post("/api/auth/login");
        if (res.getStatusCode() != 200) return null;
        String token = res.jsonPath().getString("data.token");
        return token != null ? token : res.jsonPath().getString("token");
    }

    private static byte[] pdf(String marker) {
        return ("%PDF-1.4\n% hop dong test " + marker + "\n%%EOF").getBytes(StandardCharsets.UTF_8);
    }

    private Response upload(String token, int id, String fileName, byte[] content, String mime) {
        io.restassured.specification.RequestSpecification req = RestAssured.given();
        if (token != null) req = req.header("Authorization", "Bearer " + token);
        return req.multiPart("file", fileName, content, mime).put(HR_CONTRACTS + "/students/" + id);
    }

    @BeforeClass
    public void setup() {
        RestAssured.baseURI = BASE_URL;
        hrToken = login("customer.hr@company.com");
        studentToken = login("hung.nt@gmail.com");
        Assert.assertNotNull(hrToken, "Không đăng nhập được tài khoản HR!");

        Response list = RestAssured.given().header("Authorization", "Bearer " + hrToken).get(HR_CONTRACTS);
        Assert.assertEquals(list.getStatusCode(), 200, "Không lấy được danh sách hợp đồng!");
        List<Map<String, Object>> rows = list.jsonPath().getList("data");
        for (Map<String, Object> row : rows) {
            if (!"CONFIRMED".equals(row.get("status"))) {
                studentId = ((Number) row.get("studentId")).intValue();
                break;
            }
        }
        if (studentId == 0) throw new SkipException("Mọi hợp đồng đã được xác nhận - đặt lại trạng thái rồi chạy lại.");
    }

    @Test(priority = 1)
    public void testUploadValidPdf() {
        Response res = upload(hrToken, studentId, "hop-dong-a.pdf", pdf("A-" + System.nanoTime()), "application/pdf");
        Assert.assertTrue(res.getStatusCode() == 200 || res.getStatusCode() == 201,
                "Upload hợp lệ thất bại: " + res.getStatusCode());
        Assert.assertEquals(res.jsonPath().getString("data.fileName"), "hop-dong-a.pdf");
        Assert.assertEquals(res.jsonPath().getBoolean("data.hasFile"), true);
        Assert.assertEquals(res.jsonPath().getString("data.status"), "PENDING_CONFIRMATION");
    }

    @Test(priority = 2, dependsOnMethods = "testUploadValidPdf")
    public void testDuplicateFileIsRejected() {
        byte[] content = pdf("DUP-" + System.nanoTime());
        Assert.assertTrue(upload(hrToken, studentId, "hop-dong-b.pdf", content, "application/pdf").getStatusCode() < 300);
        Response again = upload(hrToken, studentId, "hop-dong-b-copy.pdf", content, "application/pdf");
        Assert.assertEquals(again.getStatusCode(), 409);
    }

    @Test(priority = 3, dependsOnMethods = "testUploadValidPdf")
    public void testReplaceWithDifferentFileSucceeds() {
        Response res = upload(hrToken, studentId, "hop-dong-moi.pdf", pdf("NEW-" + System.nanoTime()), "application/pdf");
        Assert.assertEquals(res.getStatusCode(), 200);
        Assert.assertEquals(res.jsonPath().getString("data.fileName"), "hop-dong-moi.pdf");
    }

    @Test(priority = 4)
    public void testUnsupportedExtensionIsRejected() {
        Response res = upload(hrToken, studentId, "hop-dong.exe", pdf("EXE"), "application/octet-stream");
        Assert.assertEquals(res.getStatusCode(), 400);
    }

    @Test(priority = 5)
    public void testFakePdfWithWrongContentIsRejected() {
        byte[] notPdf = "day khong phai la pdf".getBytes(StandardCharsets.UTF_8);
        Response res = upload(hrToken, studentId, "gia-mao.pdf", notPdf, "application/pdf");
        Assert.assertEquals(res.getStatusCode(), 400);
    }

    @Test(priority = 6)
    public void testEmptyFileIsRejected() {
        Response res = upload(hrToken, studentId, "rong.pdf", new byte[0], "application/pdf");
        Assert.assertEquals(res.getStatusCode(), 400);
    }

    @Test(priority = 7)
    public void testOversizeFileIsRejected() {
        byte[] big = new byte[MAX_BYTES + 1];
        byte[] header = "%PDF-1.4\n".getBytes(StandardCharsets.UTF_8);
        System.arraycopy(header, 0, big, 0, header.length);
        Response res = upload(hrToken, studentId, "qua-lon.pdf", big, "application/pdf");
        Assert.assertTrue(res.getStatusCode() == 400 || res.getStatusCode() == 413,
                "File quá 10 MB phải bị từ chối, nhận được: " + res.getStatusCode());
    }

    @Test(priority = 8)
    public void testUploadWithoutTokenIsUnauthorized() {
        Assert.assertEquals(upload(null, studentId, "a.pdf", pdf("NOAUTH"), "application/pdf").getStatusCode(), 401);
    }

    @Test(priority = 9)
    public void testStudentRoleCannotUpload() {
        if (studentToken == null) throw new SkipException("Không đăng nhập được tài khoản sinh viên.");
        Assert.assertEquals(upload(studentToken, studentId, "a.pdf", pdf("STU"), "application/pdf").getStatusCode(), 403);
    }

    @Test(priority = 10)
    public void testUploadForUnknownStudentReturnsNotFound() {
        Assert.assertEquals(upload(hrToken, 999999, "a.pdf", pdf("404"), "application/pdf").getStatusCode(), 404);
    }

    @Test(priority = 11, dependsOnMethods = "testUploadValidPdf")
    public void testHrCanDownloadUploadedContract() {
        Response res = RestAssured.given().header("Authorization", "Bearer " + hrToken)
                .get(HR_CONTRACTS + "/students/" + studentId + "/download");
        Assert.assertEquals(res.getStatusCode(), 200);
        Assert.assertTrue(res.asByteArray().length > 0);
    }
}
