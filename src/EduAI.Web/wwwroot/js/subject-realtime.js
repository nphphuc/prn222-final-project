(function () {
    'use strict';

    var Actions = {
        Created: 'Created',
        Updated: 'Updated',
        Deleted: 'Deleted',
        Restored: 'Restored',
        TeacherAssigned: 'TeacherAssigned',
        TeacherUnassigned: 'TeacherUnassigned',
        MaterialsRemoved: 'MaterialsRemoved'
    };

    function escapeHtml(text) {
        if (!text) return '';
        var div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }

    function shouldShowSubject(subject, role, userId) {
        if (!subject) return false;
        if (role === 'Admin') return true;
        if (subject.isActive === false) return false;
        if (role === 'Teacher') return subject.teacherId === userId;
        if (role === 'Student') return (subject.documentCount || 0) > 0;
        return false;
    }

    function getActionMessage(action, subject, role, userId, previousTeacherId) {
        var name = subject && subject.name ? '"' + subject.name + '"' : 'môn học';
        switch (action) {
            case Actions.Created:
                return 'Môn học mới: ' + name + '.';
            case Actions.Restored:
                return 'Môn học ' + name + ' đã được khôi phục.';
            case Actions.Updated:
                return 'Môn học ' + name + ' đã được cập nhật.';
            case Actions.Deleted:
                return 'Môn học ' + name + ' đã bị ẩn.';
            case Actions.TeacherAssigned:
                if (role === 'Teacher' && subject && subject.teacherId === userId)
                    return 'Bạn được phân công môn ' + name + '.';
                return 'Giáo viên phụ trách môn ' + name + ' đã thay đổi.';
            case Actions.TeacherUnassigned:
                if (role === 'Teacher' && previousTeacherId === userId)
                    return 'Bạn đã được gỡ khỏi môn ' + name + '.';
                return 'Môn ' + name + ' chưa có giáo viên phụ trách.';
            case Actions.MaterialsRemoved:
                return 'Môn ' + name + ' không còn tài liệu đã index.';
            default:
                return 'Môn học ' + name + ' đã thay đổi.';
        }
    }

    function updateSubjectDetailsPage(subject) {
        if (!subject) return;
        var title = document.getElementById('subject-page-title');
        var subtitle = document.getElementById('subject-page-subtitle');
        var desc = document.getElementById('subject-description');
        var docCount = document.getElementById('subject-doc-count');
        if (title) title.textContent = subject.name || '';
        if (subtitle) {
            subtitle.textContent = subject.teacherName
                ? 'GV: ' + subject.teacherName
                : 'Chưa gán giáo viên';
        }
        if (desc) desc.textContent = subject.description || '—';
        if (docCount) docCount.textContent = String(subject.documentCount || 0);
    }

    function buildTeacherCell(subject, role) {
        if (role !== 'Admin') {
            return escapeHtml(subject.teacherName || '—');
        }
        if (subject.teacherId && subject.teacherName) {
            return '<span class="badge badge-teacher-assigned">' + escapeHtml(subject.teacherName) + '</span>' +
                '<button type="button" class="badge badge-assign-teacher border-0 js-assign-teacher ms-1" ' +
                'data-subject-id="' + subject.id + '" data-subject-name="' + escapeHtml(subject.name) + '" ' +
                'data-teacher-id="' + escapeHtml(subject.teacherId) + '">Đổi</button>';
        }
        return '<button type="button" class="badge badge-assign-teacher border-0 js-assign-teacher" ' +
            'data-subject-id="' + subject.id + '" data-subject-name="' + escapeHtml(subject.name) + '" ' +
            'data-teacher-id="">Gán giáo viên</button>';
    }

    function buildStatusCell(subject, role) {
        if (role !== 'Admin') return '';
        if (subject.isActive !== false) {
            return '<span class="badge bg-success-subtle text-success-emphasis">Hiển thị</span>';
        }
        return '<span class="badge bg-secondary-subtle text-secondary-emphasis">Đã ẩn</span>';
    }

    function buildHideRestoreButtons(subject, role) {
        if (role !== 'Admin') return '';
        if (subject.isActive !== false) {
            return '<form method="post" action="?handler=HideSubject" class="d-inline">' +
                '<input type="hidden" name="subjectId" value="' + subject.id + '" />' +
                '<button type="submit" class="btn btn-sm btn-outline-warning" ' +
                'onclick="return confirm(\'Ẩn môn này? Giáo viên và sinh viên sẽ không còn thấy môn.\');">Ẩn môn</button></form>';
        }
        return '<form method="post" action="?handler=RestoreSubject" class="d-inline">' +
            '<input type="hidden" name="subjectId" value="' + subject.id + '" />' +
            '<button type="submit" class="btn btn-sm btn-outline-success">Khôi phục</button></form>';
    }

    function buildActionButtons(subject, role, userId, urls) {
        var id = subject.id;
        var html = '<a href="' + urls.details + '?id=' + id + '" class="btn btn-sm btn-outline-primary">Chi tiết</a> ';

        if (role === 'Admin') {
            html += '<a href="' + urls.edit + '?id=' + id + '" class="btn btn-sm btn-outline-secondary">Sửa</a> ';
            html += buildHideRestoreButtons(subject, role) + ' ';
        }

        if (role === 'Teacher' && urls.upload && subject.teacherId === userId) {
            html += '<a href="' + urls.upload + '?subjectId=' + id + '" class="btn btn-sm btn-primary">Tải lên</a>';
        }

        if (role === 'Student') {
            if ((subject.documentCount || 0) > 0) {
                html += '<a href="' + urls.materials + '?subjectId=' + id + '" class="btn btn-sm btn-outline-success">Tải về</a> ';
            }
            if (subject.hasMaterials) {
                html += '<a href="' + urls.chatCreate + '?subjectId=' + id + '" class="btn btn-sm btn-primary">Chat</a>';
            }
        }

        return html;
    }

    function buildMaterialsCell(subject, role, urls) {
        var count = subject.documentCount || 0;
        if (count <= 0) return '<span class="text-muted">Chưa có tài liệu</span>';
        return '<a href="' + urls.details + '?id=' + subject.id + '" class="badge badge-doc badge-doc-link text-decoration-none">' + count + ' tài liệu</a>';
    }

    function buildSubjectRow(subject, role, userId, urls) {
        var teacherId = subject.teacherId || '';
        var tr = document.createElement('tr');
        tr.setAttribute('data-subject-id', subject.id);
        tr.setAttribute('data-teacher-id', teacherId);
        tr.setAttribute('data-has-materials', subject.hasMaterials ? 'true' : 'false');
        tr.setAttribute('data-is-active', subject.isActive !== false ? 'true' : 'false');
        if (role === 'Admin' && subject.isActive === false) {
            tr.classList.add('subject-row-inactive');
        }
        var statusCol = role === 'Admin'
            ? '<td>' + buildStatusCell(subject, role) + '</td>'
            : '';
        tr.innerHTML =
            '<td><strong>' + escapeHtml(subject.name) + '</strong></td>' +
            '<td>' + buildTeacherCell(subject, role) + '</td>' +
            '<td class="text-muted">' + escapeHtml(subject.description || '—') + '</td>' +
            '<td>' + buildMaterialsCell(subject, role, urls) + '</td>' +
            statusCol +
            '<td class="text-nowrap">' + buildActionButtons(subject, role, userId, urls) + '</td>';
        return tr;
    }

    function updateSubjectRow(row, subject, role, userId, urls) {
        row.setAttribute('data-teacher-id', subject.teacherId || '');
        row.setAttribute('data-has-materials', subject.hasMaterials ? 'true' : 'false');
        row.setAttribute('data-is-active', subject.isActive !== false ? 'true' : 'false');
        row.classList.toggle('subject-row-inactive', role === 'Admin' && subject.isActive === false);
        row.children[0].innerHTML = '<strong>' + escapeHtml(subject.name) + '</strong>';
        row.children[1].innerHTML = buildTeacherCell(subject, role);
        row.children[2].textContent = subject.description || '—';
        row.children[3].innerHTML = buildMaterialsCell(subject, role, urls);
        if (role === 'Admin') {
            row.children[4].innerHTML = buildStatusCell(subject, role);
            row.children[5].innerHTML = buildActionButtons(subject, role, userId, urls);
        } else {
            row.children[4].innerHTML = buildActionButtons(subject, role, userId, urls);
        }
    }

    function removeSubjectRows(tbody, subjectId) {
        tbody.querySelectorAll('tr[data-subject-id="' + subjectId + '"]').forEach(function (row) {
            row.remove();
        });
    }

    function toggleEmptyState(panel, emptyAlert, tbody) {
        var hasRows = tbody.querySelectorAll('tr[data-subject-id]').length > 0;
        if (emptyAlert) emptyAlert.classList.toggle('d-none', hasRows);
        if (panel) panel.classList.toggle('d-none', !hasRows);
    }

    function showToast(message) {
        var toast = document.getElementById('subject-realtime-toast');
        if (!toast) return;
        toast.textContent = message;
        toast.classList.remove('d-none');
        clearTimeout(toast._hideTimer);
        toast._hideTimer = setTimeout(function () {
            toast.classList.add('d-none');
        }, 4500);
    }

    function handleDetailsPageEvent(evt, role, userId, urls) {
        var action = evt.action;
        var subjectId = evt.subjectId;
        var subject = evt.subject;
        var previousTeacherId = evt.previousTeacherId;
        var currentSubjectId = evt.currentSubjectId;

        if (subjectId !== currentSubjectId) return false;

        if (action === Actions.Deleted) {
            showToast('Môn học này đã bị ẩn.');
            if (role === 'Admin') {
                setTimeout(function () { window.location.reload(); }, 800);
                return true;
            }
            setTimeout(function () {
                window.location.href = urls.index || '/Subjects';
            }, 1200);
            return true;
        }

        if (!shouldShowSubject(subject, role, userId)) {
            showToast('Môn học này không còn khả dụng với bạn.');
            setTimeout(function () {
                window.location.href = urls.index || '/Subjects';
            }, 1200);
            return true;
        }

        updateSubjectDetailsPage(subject);
        showToast(getActionMessage(action, subject, role, userId, previousTeacherId));
        return true;
    }

    window.initSubjectRealtime = function (options) {
        if (!options.hubUrl || !window.signalR) return;

        var role = options.role;
        var userId = options.userId || '';
        var urls = options.urls || {};
        var tbody = document.getElementById('subjects-tbody');
        var panel = document.getElementById('subjects-panel');
        var emptyAlert = document.getElementById('subjects-empty');
        var currentSubjectId = options.currentSubjectId || null;

        if (!tbody && !currentSubjectId) return;

        var connection = new signalR.HubConnectionBuilder()
            .withUrl(options.hubUrl)
            .withAutomaticReconnect()
            .build();

        connection.on('SubjectChanged', function (evt) {
            var action = evt.action;
            var subjectId = evt.subjectId;
            var subject = evt.subject;
            var previousTeacherId = evt.previousTeacherId;

            if (currentSubjectId) {
                evt.currentSubjectId = currentSubjectId;
                if (handleDetailsPageEvent(evt, role, userId, urls)) return;
            }

            if (!tbody) return;

            if (action === Actions.Deleted) {
                if (role === 'Admin' && subject) {
                    var deletedRow = tbody.querySelector('tr[data-subject-id="' + subjectId + '"]:not(.chunk-expand-row)');
                    if (deletedRow) {
                        updateSubjectRow(deletedRow, subject, role, userId, urls);
                    } else if (shouldShowSubject(subject, role, userId)) {
                        tbody.appendChild(buildSubjectRow(subject, role, userId, urls));
                        toggleEmptyState(panel, emptyAlert, tbody);
                    }
                    showToast(getActionMessage(action, subject, role, userId, previousTeacherId));
                    return;
                }
                removeSubjectRows(tbody, subjectId);
                toggleEmptyState(panel, emptyAlert, tbody);
                showToast(getActionMessage(action, subject, role, userId, previousTeacherId));
                return;
            }

            if (action === Actions.MaterialsRemoved && role === 'Student') {
                removeSubjectRows(tbody, subjectId);
                toggleEmptyState(panel, emptyAlert, tbody);
                showToast(getActionMessage(action, subject, role, userId, previousTeacherId));
                return;
            }

            if (role === 'Teacher' && (!subject || !shouldShowSubject(subject, role, userId))) {
                var teacherRow = tbody.querySelector('tr[data-subject-id="' + subjectId + '"]:not(.chunk-expand-row)');
                if (teacherRow) {
                    removeSubjectRows(tbody, subjectId);
                    toggleEmptyState(panel, emptyAlert, tbody);
                    showToast(getActionMessage(action, subject, role, userId, previousTeacherId));
                }
                return;
            }

            if (!subject || !shouldShowSubject(subject, role, userId)) {
                return;
            }

            var existing = tbody.querySelector('tr[data-subject-id="' + subject.id + '"]:not(.chunk-expand-row)');
            if (existing) {
                updateSubjectRow(existing, subject, role, userId, urls);
            } else {
                tbody.appendChild(buildSubjectRow(subject, role, userId, urls));
                toggleEmptyState(panel, emptyAlert, tbody);
            }
            showToast(getActionMessage(action, subject, role, userId, previousTeacherId));
        });

        connection.onreconnected(function () {
            return connection.invoke('JoinSubjectsFeed');
        });

        connection.start()
            .then(function () { return connection.invoke('JoinSubjectsFeed'); })
            .catch(function (err) {
                console.warn('Subject realtime connection failed:', err);
            });
    };
})();
