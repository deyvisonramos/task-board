export function toDateTimeLocalValue(isoValue: string) {
  const date = new Date(isoValue)

  if (Number.isNaN(date.getTime())) {
    return ''
  }

  const timezoneOffsetMilliseconds = date.getTimezoneOffset() * 60_000
  return new Date(date.getTime() - timezoneOffsetMilliseconds)
    .toISOString()
    .slice(0, 16)
}

export function getDefaultDueDateInput() {
  const tomorrow = new Date()
  tomorrow.setDate(tomorrow.getDate() + 1)
  tomorrow.setMinutes(0, 0, 0)

  return toDateTimeLocalValue(tomorrow.toISOString())
}

export function dateTimeLocalToUtcIso(value: string) {
  const date = new Date(value)

  if (Number.isNaN(date.getTime())) {
    return null
  }

  return date.toISOString()
}

export function formatDueDate(isoValue: string) {
  return new Intl.DateTimeFormat(undefined, {
    dateStyle: 'medium',
    timeStyle: 'short',
  }).format(new Date(isoValue))
}
