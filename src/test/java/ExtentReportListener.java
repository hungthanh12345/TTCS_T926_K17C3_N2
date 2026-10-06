import com.aventstack.extentreports.ExtentReports;
import com.aventstack.extentreports.ExtentTest;
import com.aventstack.extentreports.reporter.ExtentSparkReporter;
import com.aventstack.extentreports.reporter.configuration.Theme;
import org.testng.ITestContext;
import org.testng.ITestListener;
import org.testng.ITestResult;

public class ExtentReportListener implements ITestListener {

    private static ExtentReports extent;

    @Override
    public void onStart(ITestContext context) {
        ExtentSparkReporter sparkReporter = new ExtentSparkReporter("target/ExtentReport.html");
        sparkReporter.config().setDocumentTitle("Automation Test Report - Sprint 1");
        sparkReporter.config().setReportName("Kết Quả Kiểm Thử API Hệ Thống Quản Lý Thực Tập");
        sparkReporter.config().setTheme(Theme.STANDARD);

        extent = new ExtentReports();
        extent.attachReporter(sparkReporter);
        extent.setSystemInfo("Hệ thống", "Internship Management System");
        extent.setSystemInfo("Môi trường", "Local Host");
        extent.setSystemInfo("Tester", "Nguyễn Thị Giang / Nguyễn Thành Hưng");
    }

    @Override
    public void onTestSuccess(ITestResult result) {
        ExtentTest extentTest = extent.createTest(result.getMethod().getMethodName());
        extentTest.pass("Test case trôi qua thành công!");
    }

    @Override
    public void onTestFailure(ITestResult result) {
        ExtentTest extentTest = extent.createTest(result.getMethod().getMethodName());
        extentTest.fail(result.getThrowable());
    }

    @Override
    public void onFinish(ITestContext context) {
        if (extent != null) {
            extent.flush();
        }
    }
}