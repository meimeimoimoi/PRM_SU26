// BE (ExceptionHandlingMiddleware) luôn trả lỗi dạng { success: false, errors: string[] }.
// Helper này rút gọn về 1 message hiển thị được, dùng chung cho mọi trang gọi apiClient.
export const getErrorMessage = (error: any, fallback: string): string => {
  const errors = error?.response?.data?.errors;
  if (Array.isArray(errors) && errors.length > 0) {
    return errors[0];
  }
  return error?.message || fallback;
};
