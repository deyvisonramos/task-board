import { useEffect, useMemo, useRef, useState } from 'react'
import { getFriendlyApiError } from '../api/apiError'
import {
  tasksApi,
  type CreateTaskRequest,
  type TaskResponse,
  type TaskStatus,
} from '../api/tasksApi'
import { useAuth } from '../auth/useAuth'
import { TaskForm, type TaskFormValues } from '../components/tasks/TaskForm'
import { TaskList } from '../components/tasks/TaskList'
import { dateTimeLocalToUtcIso } from '../components/tasks/taskDate'

function sortTasks(tasks: TaskResponse[]) {
  return [...tasks].sort((first, second) => {
    const dueDateComparison =
      new Date(first.dueDate).getTime() - new Date(second.dueDate).getTime()

    if (dueDateComparison !== 0) {
      return dueDateComparison
    }

    return first.title.localeCompare(second.title)
  })
}

function toTaskRequest(values: TaskFormValues): CreateTaskRequest | null {
  const dueDate = dateTimeLocalToUtcIso(values.dueDate)

  if (!dueDate) {
    return null
  }

  return {
    title: values.title.trim(),
    description: values.description.trim() || null,
    dueDate,
    status: values.status,
  }
}

async function fetchTaskList() {
  const response = await tasksApi.list()
  return sortTasks(response.data)
}

export function DashboardPage() {
  const { user } = useAuth()
  const [tasks, setTasks] = useState<TaskResponse[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [isCreating, setIsCreating] = useState(false)
  const [busyTaskId, setBusyTaskId] = useState<string | null>(null)
  const [draggedTask, setDraggedTask] = useState<TaskResponse | null>(null)
  const [modalMode, setModalMode] = useState<'create' | 'edit' | null>(null)
  const [modalTask, setModalTask] = useState<TaskResponse | null>(null)
  const [deleteCandidate, setDeleteCandidate] = useState<TaskResponse | null>(
    null,
  )
  const [error, setError] = useState<string | null>(null)
  const [formError, setFormError] = useState<string | null>(null)
  const modalPanelRef = useRef<HTMLDivElement>(null)
  const previousFocusRef = useRef<HTMLElement | null>(null)

  const modalTitle = useMemo(() => {
    if (modalMode === 'edit') {
      return 'Edit task'
    }

    return 'Create task'
  }, [modalMode])

  useEffect(() => {
    let isMounted = true

    async function loadInitialTasks() {
      try {
        const nextTasks = await fetchTaskList()

        if (isMounted) {
          setTasks(nextTasks)
        }
      } catch (requestError) {
        if (isMounted) {
          setError(getFriendlyApiError(requestError))
        }
      } finally {
        if (isMounted) {
          setIsLoading(false)
        }
      }
    }

    void loadInitialTasks()

    return () => {
      isMounted = false
    }
  }, [])

  useEffect(() => {
    const hasOpenDialog = Boolean(modalMode || deleteCandidate)

    if (!hasOpenDialog) {
      return
    }

    previousFocusRef.current =
      document.activeElement instanceof HTMLElement
        ? document.activeElement
        : null
    modalPanelRef.current?.focus()

    function handleKeyDown(event: KeyboardEvent) {
      if (event.key === 'Escape') {
        closeActiveDialog()
      }
    }

    document.addEventListener('keydown', handleKeyDown)

    return () => {
      document.removeEventListener('keydown', handleKeyDown)
      previousFocusRef.current?.focus()
      previousFocusRef.current = null
    }
  }, [modalMode, deleteCandidate])

  async function handleRefresh() {
    setIsLoading(true)
    setError(null)

    try {
      const nextTasks = await fetchTaskList()
      setTasks(nextTasks)
    } catch (requestError) {
      setError(getFriendlyApiError(requestError))
    } finally {
      setIsLoading(false)
    }
  }

  async function handleCreateTask(values: TaskFormValues) {
    setFormError(null)
    const request = toTaskRequest(values)

    if (!request) {
      setFormError('Enter a valid due date.')
      return false
    }

    setIsCreating(true)

    try {
      const response = await tasksApi.create(request)
      setTasks((current) => sortTasks([...current, response.data]))
      setModalMode(null)
      setModalTask(null)
      return true
    } catch (requestError) {
      setFormError(getFriendlyApiError(requestError))
      return false
    } finally {
      setIsCreating(false)
    }
  }

  async function handleUpdateTask(task: TaskResponse, values: TaskFormValues) {
    setError(null)
    const request = toTaskRequest(values)

    if (!request) {
      setError('Enter a valid due date.')
      return false
    }

    setBusyTaskId(task.id)

    try {
      const response = await tasksApi.update(task.id, request)
      setTasks((current) =>
        sortTasks(
          current.map((currentTask) =>
            currentTask.id === task.id ? response.data : currentTask,
          ),
        ),
      )
      setModalMode(null)
      setModalTask(null)
      return true
    } catch (requestError) {
      setError(getFriendlyApiError(requestError))
      return false
    } finally {
      setBusyTaskId(null)
    }
  }

  async function handleStatusChange(task: TaskResponse, status: TaskStatus) {
    if (task.status === status) {
      setDraggedTask(null)
      return
    }

    setError(null)
    setBusyTaskId(task.id)

    try {
      const response = await tasksApi.update(task.id, {
        title: task.title,
        description: task.description,
        dueDate: task.dueDate,
        status,
      })
      setTasks((current) =>
        sortTasks(
          current.map((currentTask) =>
            currentTask.id === task.id ? response.data : currentTask,
          ),
        ),
      )
    } catch (requestError) {
      setError(getFriendlyApiError(requestError))
    } finally {
      setBusyTaskId(null)
      setDraggedTask(null)
    }
  }

  async function handleDropTask(status: TaskStatus) {
    if (!draggedTask) {
      return
    }

    await handleStatusChange(draggedTask, status)
  }

  async function requestDeleteTask(task: TaskResponse) {
    setDeleteCandidate(task)
  }

  async function confirmDeleteTask() {
    if (!deleteCandidate) {
      return
    }

    const task = deleteCandidate
    setError(null)
    setBusyTaskId(task.id)

    try {
      await tasksApi.remove(task.id)
      setTasks((current) =>
        current.filter((currentTask) => currentTask.id !== task.id),
      )

      if (modalTask?.id === task.id) {
        setModalMode(null)
        setModalTask(null)
      }

      setDeleteCandidate(null)
    } catch (requestError) {
      setError(getFriendlyApiError(requestError))
    } finally {
      setBusyTaskId(null)
    }
  }

  function openCreateModal() {
    setFormError(null)
    setModalTask(null)
    setModalMode('create')
  }

  function openEditModal(task: TaskResponse) {
    setFormError(null)
    setModalTask(task)
    setModalMode('edit')
  }

  function closeTaskModal() {
    setFormError(null)
    setModalMode(null)
    setModalTask(null)
  }

  function closeDeleteDialog() {
    setDeleteCandidate(null)
  }

  function closeActiveDialog() {
    if (deleteCandidate) {
      closeDeleteDialog()
      return
    }

    closeTaskModal()
  }

  return (
    <section className="dashboard-page" aria-labelledby="dashboard-title">
      <div className="dashboard-header">
        <div>
          <p className="page-kicker">Tasks</p>
          <h1 id="dashboard-title" className="page-title">
            Dashboard
          </h1>
          <p className="page-description">
            Welcome, {user?.email}. Manage your task list across the board.
          </p>
        </div>

        <button className="primary-button" onClick={openCreateModal} type="button">
          Create task
        </button>
      </div>

      <div className="dashboard-layout">
        <section className="task-list-panel" aria-labelledby="task-list-title">
          <div className="task-list-heading">
            <div>
              <p className="page-kicker">Board</p>
              <h2 id="task-list-title" className="panel-title">
                Your tasks
              </h2>
            </div>

            <button
              className="secondary-button"
              disabled={isLoading}
              onClick={() => void handleRefresh()}
              type="button"
            >
              Refresh
            </button>
          </div>

          {error ? (
            <div className="form-error" role="alert">
              {error}
            </div>
          ) : null}

          {isLoading ? (
            <div className="task-loading-state" role="status">
              Loading tasks...
            </div>
          ) : (
            <TaskList
              busyTaskId={busyTaskId}
              onDelete={requestDeleteTask}
              onDragEnd={() => setDraggedTask(null)}
              onDragStart={setDraggedTask}
              onDropTask={handleDropTask}
              onEdit={openEditModal}
              onStatusChange={handleStatusChange}
              tasks={tasks}
            />
          )}
        </section>
      </div>

      {modalMode ? (
        <div
          aria-labelledby="task-modal-title"
          aria-modal="true"
          className="modal-backdrop"
          role="dialog"
        >
          <div className="modal-panel" ref={modalPanelRef} tabIndex={-1}>
            <div className="modal-header">
              <div>
                <p className="page-kicker">Task details</p>
                <h2 id="task-modal-title" className="panel-title">
                  {modalTitle}
                </h2>
              </div>
              <button
                aria-label="Close task form"
                className="icon-button"
                onClick={closeTaskModal}
                type="button"
              >
                x
              </button>
            </div>

            {formError ? (
              <div className="form-error" role="alert">
                {formError}
              </div>
            ) : null}

            <TaskForm
              initialTask={modalTask ?? undefined}
              isSubmitting={
                modalMode === 'create'
                  ? isCreating
                  : Boolean(modalTask && busyTaskId === modalTask.id)
              }
              mode={modalMode}
              onCancel={closeTaskModal}
              onSubmit={(values) =>
                modalMode === 'create'
                  ? handleCreateTask(values)
                  : modalTask
                    ? handleUpdateTask(modalTask, values)
                    : Promise.resolve(false)
              }
            />
          </div>
        </div>
      ) : null}

      {deleteCandidate ? (
        <div
          aria-labelledby="delete-modal-title"
          aria-modal="true"
          className="modal-backdrop"
          role="dialog"
        >
          <div className="modal-panel modal-panel-compact" ref={modalPanelRef} tabIndex={-1}>
            <div className="modal-header">
              <div>
                <p className="page-kicker">Delete task</p>
                <h2 id="delete-modal-title" className="panel-title">
                  Confirm delete
                </h2>
              </div>
              <button
                aria-label="Close delete confirmation"
                className="icon-button"
                onClick={closeDeleteDialog}
                type="button"
              >
                x
              </button>
            </div>

            <p className="page-description">
              Delete "{deleteCandidate.title}"?
            </p>

            <div className="task-form-actions">
              <button
                className="secondary-button"
                disabled={busyTaskId === deleteCandidate.id}
                onClick={closeDeleteDialog}
                type="button"
              >
                Cancel
              </button>
              <button
                className="danger-button"
                disabled={busyTaskId === deleteCandidate.id}
                onClick={() => void confirmDeleteTask()}
                type="button"
              >
                Delete
              </button>
            </div>
          </div>
        </div>
      ) : null}
    </section>
  )
}
