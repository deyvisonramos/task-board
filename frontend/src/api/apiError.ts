import axios from 'axios'

type ApiValidationError = {
  code?: string
  message?: string
}

type ApiErrorResponse = {
  code?: string
  message?: string
  validation?: ApiValidationError[]
}

export function getFriendlyApiError(error: unknown) {
  if (!axios.isAxiosError<ApiErrorResponse>(error)) {
    return 'Something went wrong. Please try again.'
  }

  if (!error.response) {
    return 'We could not reach the API. Make sure it is running and try again.'
  }

  const validationMessage = error.response.data?.validation
    ?.map((item) => item.message)
    .find((message): message is string => Boolean(message))

  return (
    validationMessage ??
    error.response.data?.message ??
    'The request could not be completed. Please try again.'
  )
}
