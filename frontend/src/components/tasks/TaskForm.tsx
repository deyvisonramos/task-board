import { useState, type FormEvent } from 'react'
import {
  TASK_STATUSES,
  type TaskResponse,
  type TaskStatus,
} from '../../api/tasksApi'
import { getDefaultDueDateInput, toDateTimeLocalValue } from './taskDate'

export type TaskFormValues = {
  title: string
  description: string
  status: TaskStatus
  dueDate: string
}

type TaskFormProps = {
  initialTask?: TaskResponse
  isSubmitting: boolean
  mode: 'create' | 'edit'
  onCancel?: () => void
  onSubmit: (values: TaskFormValues) => Promise<boolean>
}

function buildInitialValues(task?: TaskResponse): TaskFormValues {
  if (!task) {
    return {
      title: '',
      description: '',
      status: 'Todo',
      dueDate: getDefaultDueDateInput(),
    }
  }

  return {
    title: task.title,
    description: task.description ?? '',
    status: task.status,
    dueDate: toDateTimeLocalValue(task.dueDate),
  }
}

function submitLabel(mode: TaskFormProps['mode'], isSubmitting: boolean) {
  if (isSubmitting) {
    return mode === 'create' ? 'Creating...' : 'Saving...'
  }

  return mode === 'create' ? 'Create task' : 'Save changes'
}

export function TaskForm({
  initialTask,
  isSubmitting,
  mode,
  onCancel,
  onSubmit,
}: TaskFormProps) {
  const [values, setValues] = useState<TaskFormValues>(() =>
    buildInitialValues(initialTask),
  )

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()

    const didSubmit = await onSubmit(values)

    if (didSubmit && mode === 'create') {
      setValues(buildInitialValues())
    }
  }

  return (
    <form className="task-form" onSubmit={handleSubmit}>
      <div className="task-form-grid">
        <label className="form-field">
          <span>Title</span>
          <input
            disabled={isSubmitting}
            maxLength={100}
            name="title"
            onChange={(event) =>
              setValues((current) => ({
                ...current,
                title: event.target.value,
              }))
            }
            required
            type="text"
            value={values.title}
          />
        </label>

        <label className="form-field">
          <span>Status</span>
          <select
            disabled={isSubmitting}
            name="status"
            onChange={(event) =>
              setValues((current) => ({
                ...current,
                status: event.target.value as TaskStatus,
              }))
            }
            value={values.status}
          >
            {TASK_STATUSES.map((status) => (
              <option key={status} value={status}>
                {status}
              </option>
            ))}
          </select>
        </label>
      </div>

      <label className="form-field">
        <span>Due date</span>
        <input
          disabled={isSubmitting}
          name="dueDate"
          onChange={(event) =>
            setValues((current) => ({
              ...current,
              dueDate: event.target.value,
            }))
          }
          required
          type="datetime-local"
          value={values.dueDate}
        />
      </label>

      <label className="form-field">
        <span>Description</span>
        <textarea
          disabled={isSubmitting}
          maxLength={1000}
          name="description"
          onChange={(event) =>
            setValues((current) => ({
              ...current,
              description: event.target.value,
            }))
          }
          rows={4}
          value={values.description}
        />
      </label>

      <div className="task-form-actions">
        {onCancel ? (
          <button
            className="secondary-button"
            disabled={isSubmitting}
            onClick={onCancel}
            type="button"
          >
            Cancel
          </button>
        ) : null}
        <button className="primary-button" disabled={isSubmitting} type="submit">
          {submitLabel(mode, isSubmitting)}
        </button>
      </div>
    </form>
  )
}
