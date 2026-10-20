const localizedServerMessages = new Map([
  [400, 'Thông tin gửi lên chưa hợp lệ. Vui lòng kiểm tra và thử lại.'],
  [401, 'Phiên đăng nhập không hợp lệ hoặc đã hết hạn. Vui lòng đăng nhập lại.'],
  [403, 'Bạn không có quyền thực hiện thao tác này.'],
  [404, 'Không tìm thấy dữ liệu hoặc chức năng được yêu cầu.'],
  [409, 'Dữ liệu đang xung đột với yêu cầu hiện tại. Vui lòng tải lại và thử lại.'],
  [422, 'Dữ liệu chưa đáp ứng yêu cầu. Vui lòng kiểm tra và thử lại.'],
]);

const containsVietnamese = (value) => /[À-ỹĐđ]/u.test(value);

export const apiErrorMessage = (error, fallback = 'Không thể hoàn thành yêu cầu.') => {
  const response = error?.response;
  const serverMessage = response?.data?.message ?? response?.data?.title;

  if (typeof serverMessage === 'string' && containsVietnamese(serverMessage)) {
    return serverMessage;
  }

  if (!response) {
    if (typeof error?.message === 'string' && containsVietnamese(error.message)) {
      return error.message;
    }

    if (error?.request || error?.isAxiosError) {
      return 'Không thể kết nối đến máy chủ. Vui lòng kiểm tra kết nối và thử lại.';
    }

    return fallback;
  }

  if (localizedServerMessages.has(response.status)) {
    return localizedServerMessages.get(response.status);
  }

  if (response.status >= 500) {
    return 'Đã xảy ra lỗi máy chủ. Vui lòng thử lại sau.';
  }

  return fallback;
};

export default apiErrorMessage;
