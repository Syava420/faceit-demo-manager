// App State
const state = {
  activeTab: 'importTab',
  selectedCategory: 'General',
  selectedMapFilter: null,
  demos: [],
  categories: ['General'],
  binds: [],
  settings: {},
  selectedDemos: [],
  selectedDemoPath: null,
  lastClickedDemoPath: null
};

// DOM Elements
const elements = {
  btnWinMinimize: document.getElementById('btnWinMinimize'),
  btnWinClose: document.getElementById('btnWinClose'),
  navItems: document.querySelectorAll('.nav-item'),
  tabPanes: document.querySelectorAll('.tab-pane'),
  dropZone: document.getElementById('dropZone'),
  txtDownloads: document.getElementById('txtDownloads'),
  txtCS2: document.getElementById('txtCS2'),
  txtNickname: document.getElementById('txtNickname'),
  cboImportMode: document.getElementById('cboImportMode'),
  cboImportFolder: document.getElementById('cboImportFolder'),
  chkWatchFolder: document.getElementById('chkWatchFolder'),
  chkTray: document.getElementById('chkTray'),
  chkVoiceInDemos: document.getElementById('chkVoiceInDemos'),
  chkDeleteArchives: document.getElementById('chkDeleteArchives'),
  txtLogConsole: document.getElementById('txtLogConsole'),
  tblDemoBody: document.getElementById('tblDemoBody'),
  tblBindsBody: document.getElementById('tblBindsBody'),
  categoriesList: document.getElementById('categoriesList'),
  pnlMapFilters: document.getElementById('pnlMapFilters'),
  txtSearch: document.getElementById('txtSearch'),
  lblStatus: document.getElementById('lblStatus'),
  prgBarFill: document.getElementById('prgBarFill'),
  userNickDisplay: document.getElementById('userNickDisplay'),
  userEloDisplay: document.getElementById('userEloDisplay'),
  userLevelBadge: document.getElementById('userLevelBadge'),
  userAvatar: document.getElementById('userAvatar')
};

// Initialize App
document.addEventListener('DOMContentLoaded', () => {
  setupNavigation();
  setupWindowControls();
  setupDragAndDrop();
  setupEventListeners();

  if (window.chrome && window.chrome.webview) {
    window.chrome.webview.addEventListener('message', event => {
      window.onNativeEvent(event.data);
    });
  }

  // Request initial state from C# Native Host
  postNativeMessage({ action: 'initApp' });
});

// Post IPC Message to C# WebView2
function postNativeMessage(data) {
  if (window.chrome && window.chrome.webview) {
    window.chrome.webview.postMessage(data);
  } else {
    console.log('Native IPC Call:', data);
  }
}

// Global Event Handler called by C# Host
window.onNativeEvent = function(event) {
  if (!event || !event.type) return;

  switch (event.type) {
    case 'updateProfile':
      if (elements.userNickDisplay) elements.userNickDisplay.textContent = event.nickname || 'Unknown';
      if (elements.userEloDisplay) elements.userEloDisplay.textContent = event.elo || '----';
      if (elements.userLevelBadge) elements.userLevelBadge.textContent = event.level || '--';
      if (elements.userAvatar && event.avatar) elements.userAvatar.src = event.avatar;
      break;

    case 'updateSettings':
      if (event.settings) {
        state.settings = event.settings;
        const txtDl = document.getElementById('txtDownloads');
        const txtCs = document.getElementById('txtCS2');
        const txtNick = document.getElementById('txtNickname');
        const chkWf = document.getElementById('chkWatchFolder');
        const chkTray = document.getElementById('chkTray');
        const chkVoice = document.getElementById('chkVoiceInDemos');
        const chkDel = document.getElementById('chkDeleteArchives');
        const cboMode = document.getElementById('cboImportMode');
        const cboFolder = document.getElementById('cboImportFolder');

        if (txtDl) txtDl.value = event.settings.downloadsPath || '';
        if (txtCs) txtCs.value = event.settings.cs2Path || '';
        if (chkWf) chkWf.checked = !!event.settings.watchFolder;
        if (chkTray) chkTray.checked = !!event.settings.minimizeTray;
        if (chkVoice) chkVoice.checked = !!event.settings.enableDemoVoice;
        if (chkDel) chkDel.checked = !!event.settings.deleteArchivesAfterUnpack;
        if (cboMode && event.settings.importMode) cboMode.value = event.settings.importMode;
        if (cboFolder && event.settings.targetImportFolder) cboFolder.value = event.settings.targetImportFolder;

        if (txtNick) {
          const folder = cboFolder ? cboFolder.value : 'General';
          txtNick.value = getFolderNickname(folder, event.settings);
        }
        
        const chkAutoBinds = document.getElementById('chkAutoApplyBinds');
        if (chkAutoBinds) chkAutoBinds.checked = !!event.settings.autoApplyBinds;

        postNativeMessage({ 
          action: 'debugLog', 
          msg: `updateSettings: CS2='${event.settings.cs2Path}', DL='${event.settings.downloadsPath}', ImportMode='${event.settings.importMode}', ImportFolder='${event.settings.targetImportFolder}'` 
        });
      }
      break;

    case 'updateDemos':
      state.demos = (event.demos || []).map(d => {
        if (d.filePath) {
          d.filePath = d.filePath.replace(/\\/g, '/');
        }
        return d;
      });
      state.selectedDemos = state.selectedDemos.filter(path => state.demos.some(d => d.filePath === path));
      state.selectedDemoPath = state.selectedDemos[0] || null;
      renderDemos();
      renderMapFilters();
      break;

    case 'updateCategories':
      state.categories = event.categories || ['General'];
      renderCategories();
      updateTargetFolderCombobox();
      break;

    case 'updateBinds':
      state.binds = event.binds || [];
      renderBinds();
      break;

    case 'appendLog':
      if (elements.txtLogConsole) {
        elements.txtLogConsole.value += event.text + '\n';
        elements.txtLogConsole.scrollTop = elements.txtLogConsole.scrollHeight;
      }
      break;

    case 'updateStatus':
      if (elements.lblStatus) elements.lblStatus.textContent = event.status || '';
      if (elements.prgBarFill) elements.prgBarFill.style.width = (event.progress || 0) + '%';
      break;

    default:
      console.log('Unhandled native event:', event);
  }
};

// Setup Tab Navigation
function setupNavigation() {
  elements.navItems.forEach(item => {
    item.addEventListener('click', () => {
      const targetTab = item.getAttribute('data-tab');
      
      elements.navItems.forEach(i => i.classList.remove('active'));
      elements.tabPanes.forEach(p => p.classList.remove('active'));

      item.classList.add('active');
      document.getElementById(targetTab).classList.add('active');
      state.activeTab = targetTab;
    });
  });
}

function switchTab(tabId) {
  const targetItem = Array.from(elements.navItems).find(i => i.getAttribute('data-tab') === tabId);
  if (targetItem) {
    elements.navItems.forEach(i => i.classList.remove('active'));
    elements.tabPanes.forEach(p => p.classList.remove('active'));

    targetItem.classList.add('active');
    document.getElementById(tabId).classList.add('active');
    state.activeTab = tabId;
  }
}

// Window Controls
function setupWindowControls() {
  if (elements.btnWinMinimize) {
    elements.btnWinMinimize.addEventListener('click', () => {
      postNativeMessage({ action: 'minimizeWindow' });
    });
  }
  if (elements.btnWinClose) {
    elements.btnWinClose.addEventListener('click', () => {
      postNativeMessage({ action: 'closeWindow' });
    });
  }
  const titlebar = document.getElementById('titlebar');
  if (titlebar) {
    titlebar.addEventListener('mousedown', (e) => {
      if (e.target.closest('.win-btn')) return;
      postNativeMessage({ action: 'dragWindow' });
    });
    titlebar.addEventListener('dblclick', (e) => {
      if (e.target.closest('.win-btn')) return;
      postNativeMessage({ action: 'maximizeWindow' });
    });
  }
}

// Drag and Drop
function setupDragAndDrop() {
  const dropZone = elements.dropZone;
  if (!dropZone) return;

  dropZone.addEventListener('click', () => {
    postNativeMessage({ action: 'browseDemosManual' });
  });

  window.addEventListener('dragover', (e) => {
    e.preventDefault();
    if (dropZone) dropZone.classList.add('dragover');
  });

  window.addEventListener('dragleave', (e) => {
    if (e.clientX <= 0 || e.clientY <= 0 || e.clientX >= window.innerWidth || e.clientY >= window.innerHeight) {
      if (dropZone) dropZone.classList.remove('dragover');
    }
  });

  window.addEventListener('drop', (e) => {
    if (dropZone) dropZone.classList.remove('dragover');
  });
}

// Attach Event Listeners
function setupEventListeners() {
  // Input settings blur/change
  const saveSetting = (skipUpdateFromFields = false) => {
    const txtDl = document.getElementById('txtDownloads');
    const txtCs = document.getElementById('txtCS2');
    const txtNick = document.getElementById('txtNickname');
    const chkWf = document.getElementById('chkWatchFolder');
    const chkTray = document.getElementById('chkTray');
    const chkVoice = document.getElementById('chkVoiceInDemos');
    const chkDel = document.getElementById('chkDeleteArchives');
    const cboMode = document.getElementById('cboImportMode');
    const cboFolder = document.getElementById('cboImportFolder');

    const folder = cboFolder ? cboFolder.value : 'General';

    if (!skipUpdateFromFields) {
      const nickVal = txtNick ? txtNick.value.trim() : '';
      if (folder && folder !== 'General') {
        if (!state.settings.folderNicknames) state.settings.folderNicknames = {};
        state.settings.folderNicknames[folder] = nickVal;
      } else {
        state.settings.nickname = nickVal;
      }
    }

    postNativeMessage({
      action: 'saveSettings',
      settings: {
        downloadsPath: txtDl ? txtDl.value.trim() : '',
        cs2Path: txtCs ? txtCs.value.trim() : '',
        nickname: state.settings.nickname || '',
        watchFolder: chkWf ? chkWf.checked : false,
        minimizeTray: chkTray ? chkTray.checked : false,
        enableDemoVoice: chkVoice ? chkVoice.checked : false,
        deleteArchivesAfterUnpack: chkDel ? chkDel.checked : false,
        autoApplyBinds: document.getElementById('chkAutoApplyBinds')?.checked || false,
        importMode: cboMode ? cboMode.value : 'General',
        targetImportFolder: folder,
        folderNicknames: state.settings.folderNicknames || {}
      }
    });
  };

function getFolderNickname(folderPath, customSettings) {
  const settings = customSettings || state.settings || {};
  if (!folderPath) return '';
  if (folderPath === 'General') {
    return settings.nickname || '';
  }
  const nicknames = settings.folderNicknames || {};
  if (nicknames[folderPath]) {
    return nicknames[folderPath];
  }
  let currentPath = folderPath;
  while (currentPath.includes('/')) {
    const lastSlash = currentPath.lastIndexOf('/');
    currentPath = currentPath.substring(0, lastSlash);
    if (nicknames[currentPath]) {
      return nicknames[currentPath];
    }
  }
  return '';
}

  const onFolderChange = () => {
    const cboFolder = document.getElementById('cboImportFolder');
    const folder = cboFolder ? cboFolder.value : 'General';
    
    // Update the nickname field value to match the selected folder's nickname
    const txtNick = document.getElementById('txtNickname');
    if (txtNick) {
      txtNick.value = getFolderNickname(folder);
    }

    saveSetting(true);
  };

  ['txtDownloads', 'txtCS2', 'txtNickname'].forEach(id => {
    const el = document.getElementById(id);
    if (el) {
      el.addEventListener('change', () => saveSetting());
      el.addEventListener('input', () => saveSetting());
    }
  });

  ['chkWatchFolder', 'chkTray', 'chkVoiceInDemos', 'chkDeleteArchives', 'chkAutoApplyBinds', 'cboImportMode'].forEach(id => {
    const el = document.getElementById(id);
    if (el) el.addEventListener('change', () => saveSetting());
  });

  const cboFolderEl = document.getElementById('cboImportFolder');
  if (cboFolderEl) cboFolderEl.addEventListener('change', onFolderChange);

  // Action Buttons
  document.getElementById('btnBrowseDownloads')?.addEventListener('click', () => postNativeMessage({ action: 'browseDownloads' }));
  document.getElementById('btnAutoCS2')?.addEventListener('click', () => postNativeMessage({ action: 'autoDetectCS2' }));
  document.getElementById('btnBrowseCS2')?.addEventListener('click', () => postNativeMessage({ action: 'browseCS2' }));
  document.getElementById('btnProcess')?.addEventListener('click', () => postNativeMessage({ action: 'processDownloads' }));
  document.getElementById('btnClearLog')?.addEventListener('click', () => { if (elements.txtLogConsole) elements.txtLogConsole.value = ''; });
  document.getElementById('btnPlay')?.addEventListener('click', playSelectedDemo);
  document.getElementById('btnNewCategory')?.addEventListener('click', () => postNativeMessage({ action: 'createCategory' }));
  document.getElementById('btnMoveDemo')?.addEventListener('click', () => {
    if (!state.selectedDemos || state.selectedDemos.length === 0) return;
    postNativeMessage({ action: 'moveSelectedDemos', filePaths: state.selectedDemos });
  });
  document.getElementById('btnDeleteDemo')?.addEventListener('click', () => {
    if (!state.selectedDemos || state.selectedDemos.length === 0) return;
    postNativeMessage({ action: 'deleteSelectedDemos', filePaths: state.selectedDemos });
  });

  // Binds Actions
  document.getElementById('btnAddBind')?.addEventListener('click', () => {
    state.binds.push({ isEnabled: true, actionName: 'Новое действие', key: '', command: '' });
    renderBinds();
    postNativeMessage({ action: 'saveBinds', binds: state.binds });
  });
  document.getElementById('btnResetBinds')?.addEventListener('click', () => {
    postNativeMessage({ action: 'resetBindsToDefault' });
  });



  // Search Input
  if (elements.txtSearch) {
    elements.txtSearch.addEventListener('input', renderDemos);
  }
}

// Render Categories
function renderCategories() {
  if (!elements.categoriesList) return;
  elements.categoriesList.innerHTML = '';

  state.categories.forEach(cat => {
    const c = typeof cat === 'string' ? { relativePath: cat, displayName: cat, depth: 0, hasChildren: false, isCollapsed: false } : cat;
    
    const item = document.createElement('div');
    item.className = `category-item ${c.relativePath === state.selectedCategory ? 'active' : ''}`;
    item.style.paddingLeft = `${c.depth * 14 + 10}px`;

    // Toggle button if has subfolders
    let toggleHtml = '';
    if (c.hasChildren) {
      toggleHtml = `<span class="category-toggle">${c.isCollapsed ? '▶' : '▼'}</span>`;
    } else {
      toggleHtml = `<span style="width: 14px; display: inline-block;"></span>`;
    }

    const folderNick = state.settings.folderNicknames && state.settings.folderNicknames[c.relativePath];
    const nickBadge = folderNick ? ` <span class="category-nick-badge">(${folderNick})</span>` : '';

    const isSelected = state.selectedCategory === c.relativePath;
    const folderColor = isSelected ? 'var(--primary)' : 'rgba(167, 139, 250, 0.6)';
    const folderSvg = `
      <svg viewBox="0 0 24 24" width="16" height="16" fill="${isSelected ? 'rgba(167, 139, 250, 0.25)' : 'none'}" stroke="${folderColor}" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round" style="vertical-align: middle; margin-right: 6px;">
        <path d="M22 19a2 2 0 01-2 2H4a2 2 0 01-2-2V5a2 2 0 012-2h5l2 3h9a2 2 0 012 2z"/>
      </svg>
    `;

    item.innerHTML = `
      ${toggleHtml}
      <span class="category-name" style="display: inline-flex; align-items: center;">${folderSvg}${c.displayName}${nickBadge}</span>
    `;

    // Click handler on the name/item
    item.addEventListener('click', (e) => {
      if (e.target.classList.contains('category-toggle')) {
        postNativeMessage({ action: 'toggleFolderCollapse', folder: c.relativePath });
        return;
      }
      state.selectedCategory = c.relativePath;
      renderCategories();
      postNativeMessage({ action: 'selectCategory', category: c.relativePath });
      switchTab('libraryTab');
    });

    // Right-click context menu handler
    item.addEventListener('contextmenu', (e) => {
      showCategoryContextMenu(e, c);
    });

    // Drag and Drop listeners
    if (c.relativePath !== 'General') {
      item.setAttribute('draggable', 'true');
      item.addEventListener('dragstart', (e) => {
        e.dataTransfer.setData('text/category-path', c.relativePath);
        e.dataTransfer.effectAllowed = 'move';
      });
    }

    item.addEventListener('dragover', (e) => {
      e.preventDefault();
      item.classList.add('drag-over');
    });

    item.addEventListener('dragleave', () => {
      item.classList.remove('drag-over');
    });

    item.addEventListener('drop', (e) => {
      item.classList.remove('drag-over');
      const rawPaths = e.dataTransfer.getData('text/demo-filepaths');
      const singleDemoPath = e.dataTransfer.getData('text/demo-filepath');
      const draggedCatPath = e.dataTransfer.getData('text/category-path');

      if (rawPaths || singleDemoPath) {
        let pathsToMove = [];
        if (rawPaths) {
          try { pathsToMove = JSON.parse(rawPaths); } catch {}
        }
        if (!pathsToMove || pathsToMove.length === 0) {
          if (singleDemoPath) pathsToMove = [singleDemoPath];
        }
        if (pathsToMove.length > 0) {
          postNativeMessage({ action: 'moveDemos', filePaths: pathsToMove, category: c.relativePath });
        }
      } else if (draggedCatPath) {
        if (draggedCatPath !== c.relativePath) {
          postNativeMessage({ action: 'moveFolder', src: draggedCatPath, dest: c.relativePath });
        }
      }
    });

    elements.categoriesList.appendChild(item);
  });
}

// Update Target Folder Combo
function updateTargetFolderCombobox() {
  if (!elements.cboImportFolder) return;
  const currentVal = elements.cboImportFolder.value || (state.settings && state.settings.targetImportFolder) || 'General';
  elements.cboImportFolder.innerHTML = '';

  state.categories.forEach(cat => {
    const c = typeof cat === 'string' ? { relativePath: cat, displayName: cat } : cat;
    const opt = document.createElement('option');
    opt.value = c.relativePath;
    opt.textContent = c.relativePath;
    elements.cboImportFolder.appendChild(opt);
  });

  elements.cboImportFolder.value = currentVal;
}

// Render Map Filter Chips
function renderMapFilters() {
  if (!elements.pnlMapFilters) return;
  elements.pnlMapFilters.innerHTML = '';

  const maps = ['Все'];
  state.demos.forEach(d => {
    if (d.map && !maps.includes(d.map)) maps.push(d.map);
  });

  maps.forEach(m => {
    const chip = document.createElement('div');
    chip.className = `chip ${(m === 'Все' && !state.selectedMapFilter) || m === state.selectedMapFilter ? 'active' : ''}`;
    chip.textContent = m;
    chip.addEventListener('click', () => {
      state.selectedMapFilter = m === 'Все' ? null : m;
      renderMapFilters();
      renderDemos();
    });
    elements.pnlMapFilters.appendChild(chip);
  });
}

// Render Demos DataGrid Table
function renderDemos() {
  if (!elements.tblDemoBody) return;
  elements.tblDemoBody.innerHTML = '';

  const query = elements.txtSearch ? elements.txtSearch.value.toLowerCase().trim() : '';

  const filtered = state.demos.filter(d => {
    const matchQuery = !query || 
      (d.map && d.map.toLowerCase().includes(query)) ||
      (d.score && d.score.toLowerCase().includes(query)) ||
      (d.note && d.note.toLowerCase().includes(query));

    const matchMap = !state.selectedMapFilter || d.map === state.selectedMapFilter;

    return matchQuery && matchMap;
  });

  filtered.forEach(d => {
    const tr = document.createElement('tr');
    tr.setAttribute('draggable', 'true');
    tr.setAttribute('data-filepath', d.filePath);
    
    if (state.selectedDemos.includes(d.filePath)) {
      tr.classList.add('selected');
    }

    let scoreHtml = d.score || '-';
    if (d.score && d.score !== '-' && d.score !== '?-?') {
      let scoreClass = 'score-normal';
      if (d.isWin === true) {
        scoreClass = 'score-win';
      } else if (d.isWin === false) {
        scoreClass = 'score-loss';
      }
      scoreHtml = `<span class="${scoreClass}">${d.score}</span>`;
    }

    const noteDisplay = d.note ? escapeHtml(d.note) : `<span class="note-placeholder">+ Заметка</span>`;

    tr.innerHTML = `
      <td><strong>${d.mapEmoji || '🗺️'} ${d.map || 'Unknown'}</strong></td>
      <td>${scoreHtml}</td>
      <td>${d.kd || '-'}</td>
      <td>${d.date || '-'}</td>
      <td class="note-cell" title="Нажмите, чтобы изменить заметку" onclick="event.stopPropagation(); editDemoNote(this, '${d.filePath}')">${noteDisplay}</td>
      <td>
        <button class="btn-secondary" onclick="event.stopPropagation(); copyDemoConfig('${d.filePath}', this)">Копировать конфиг</button>
      </td>
    `;
    tr.addEventListener('click', (e) => {
      // If Shift-click
      if (e.shiftKey && state.lastClickedDemoPath) {
        const allRows = Array.from(elements.tblDemoBody.querySelectorAll('tr'));
        const currentIndex = allRows.findIndex(r => r.getAttribute('data-filepath') === d.filePath);
        const lastIndex = allRows.findIndex(r => r.getAttribute('data-filepath') === state.lastClickedDemoPath);
        
        if (currentIndex !== -1 && lastIndex !== -1) {
          const start = Math.min(currentIndex, lastIndex);
          const end = Math.max(currentIndex, lastIndex);
          
          if (!e.ctrlKey) {
            state.selectedDemos = [];
          }
          
          for (let i = start; i <= end; i++) {
            const path = allRows[i].getAttribute('data-filepath');
            if (path && !state.selectedDemos.includes(path)) {
              state.selectedDemos.push(path);
            }
          }
        }
      }
      // If Ctrl-click (Cmd-click on Mac, but user is on Windows)
      else if (e.ctrlKey) {
        const idx = state.selectedDemos.indexOf(d.filePath);
        if (idx !== -1) {
          state.selectedDemos.splice(idx, 1);
        } else {
          state.selectedDemos.push(d.filePath);
        }
      }
      // Regular single click
      else {
        state.selectedDemos = [d.filePath];
      }

      state.selectedDemoPath = state.selectedDemos[0] || null;
      state.lastClickedDemoPath = d.filePath;
      
      // Update visual selection states
      elements.tblDemoBody.querySelectorAll('tr').forEach(r => {
        const path = r.getAttribute('data-filepath');
        if (state.selectedDemos.includes(path)) {
          r.classList.add('selected');
        } else {
          r.classList.remove('selected');
        }
      });
    });
    tr.addEventListener('dblclick', () => {
      playSingleDemo(d.filePath);
    });
    tr.addEventListener('dragstart', (e) => {
      const paths = state.selectedDemos.includes(d.filePath) ? state.selectedDemos : [d.filePath];
      e.dataTransfer.setData('text/demo-filepaths', JSON.stringify(paths));
      e.dataTransfer.setData('text/demo-filepath', d.filePath);
      e.dataTransfer.effectAllowed = 'move';
    });
    tr.addEventListener('dragover', (e) => {
      e.preventDefault();
      tr.classList.add('drag-over-row');
    });
    tr.addEventListener('dragleave', () => {
      tr.classList.remove('drag-over-row');
    });
    tr.addEventListener('drop', (e) => {
      tr.classList.remove('drag-over-row');
      const draggedFilePath = e.dataTransfer.getData('text/demo-filepath');
      if (draggedFilePath && draggedFilePath !== d.filePath) {
        const fromIdx = state.demos.findIndex(x => x.filePath === draggedFilePath);
        const toIdx = state.demos.findIndex(x => x.filePath === d.filePath);
        if (fromIdx !== -1 && toIdx !== -1) {
          const [movedItem] = state.demos.splice(fromIdx, 1);
          state.demos.splice(toIdx, 0, movedItem);
          renderDemos();
          postNativeMessage({ action: 'reorderDemos', filePaths: state.demos.map(x => x.filePath) });
        }
      }
    });
    elements.tblDemoBody.appendChild(tr);
  });

  state.selectedDemos = state.selectedDemos.filter(path => filtered.some(x => x.filePath === path));
  state.selectedDemoPath = state.selectedDemos[0] || null;
}

// Render Binds Table
function renderBinds() {
  if (!elements.tblBindsBody) return;
  elements.tblBindsBody.innerHTML = '';

  state.binds.forEach((b, idx) => {
    const tr = document.createElement('tr');
    const isEnabled = b.isEnabled !== undefined ? b.isEnabled : b.IsEnabled;
    const actionName = b.actionName || b.ActionName || b.description || '';
    const key = b.key !== undefined ? b.key : b.Key;
    const command = b.command !== undefined ? b.command : b.Command;

    tr.innerHTML = `
      <td><input type="checkbox" ${isEnabled ? 'checked' : ''} onchange="toggleBind(${idx}, this.checked)"></td>
      <td>${actionName}</td>
      <td><input type="text" value="${key || ''}" style="width: 70px;" onchange="updateBindKey(${idx}, this.value)"></td>
      <td><input type="text" value="${command || ''}" onchange="updateBindCmd(${idx}, this.value)"></td>
      <td><button class="btn-danger" onclick="deleteBind(${idx})">✕</button></td>
    `;
    elements.tblBindsBody.appendChild(tr);
  });
}

// Play Selected Demo
function playSelectedDemo() {
  if (state.selectedDemoPath) {
    postNativeMessage({ action: 'playDemo', filePath: state.selectedDemoPath });
  } else if (state.demos.length > 0) {
    postNativeMessage({ action: 'playDemo', filePath: state.demos[0].filePath });
  }
}

function playSingleDemo(filePath) {
  postNativeMessage({ action: 'playDemo', filePath: filePath });
}

function toggleBind(idx, enabled) {
  if (state.binds[idx]) state.binds[idx].isEnabled = enabled;
  postNativeMessage({ action: 'saveBinds', binds: state.binds });
}

function updateBindKey(idx, key) {
  if (state.binds[idx]) state.binds[idx].key = key;
  postNativeMessage({ action: 'saveBinds', binds: state.binds });
}

function updateBindCmd(idx, cmd) {
  if (state.binds[idx]) state.binds[idx].command = cmd;
  postNativeMessage({ action: 'saveBinds', binds: state.binds });
}

function deleteBind(idx) {
  state.binds.splice(idx, 1);
  renderBinds();
  postNativeMessage({ action: 'saveBinds', binds: state.binds });
}

let currentContextMenu = null;

function showCategoryContextMenu(e, cat) {
  e.preventDefault();
  if (currentContextMenu) currentContextMenu.remove();

  const menu = document.createElement('div');
  menu.className = 'custom-context-menu';
  menu.style.left = `${e.pageX}px`;
  menu.style.top = `${e.pageY}px`;

  // Item 1: Create Subfolder
  const createSub = document.createElement('div');
  createSub.className = 'context-menu-item';
  createSub.innerHTML = '📁 Создать подпапку';
  createSub.addEventListener('click', () => {
    postNativeMessage({ action: 'createSubfolder', parent: cat.relativePath });
    menu.remove();
  });
  menu.appendChild(createSub);

  if (cat.relativePath !== 'General') {
    // Item 1.5: Set Folder Nickname
    const setNick = document.createElement('div');
    setNick.className = 'context-menu-item';
    setNick.innerHTML = '👤 Никнейм игрока';
    setNick.addEventListener('click', () => {
      postNativeMessage({ action: 'setFolderNickname', category: cat.relativePath });
      menu.remove();
    });
    menu.appendChild(setNick);

    // Item 2: Rename
    const renameItem = document.createElement('div');
    renameItem.className = 'context-menu-item';
    renameItem.innerHTML = '✏️ Переименовать';
    renameItem.addEventListener('click', () => {
      postNativeMessage({ action: 'renameCategory', category: cat.relativePath });
      menu.remove();
    });
    menu.appendChild(renameItem);

    // Item 3: Delete
    const deleteItem = document.createElement('div');
    deleteItem.className = 'context-menu-item danger';
    deleteItem.innerHTML = '🗑️ Удалить папку';
    deleteItem.addEventListener('click', () => {
      postNativeMessage({ action: 'deleteCategory', category: cat.relativePath });
      menu.remove();
    });
    menu.appendChild(deleteItem);
  }

  // Item 4: Import Here
  const importItem = document.createElement('div');
  importItem.className = 'context-menu-item';
  importItem.innerHTML = '📥 Импортировать файлы';
  importItem.addEventListener('click', () => {
    postNativeMessage({ action: 'importFilesInto', category: cat.relativePath });
    menu.remove();
  });
  menu.appendChild(importItem);

  document.body.appendChild(menu);
  currentContextMenu = menu;

  const closeMenu = () => {
    menu.remove();
    document.removeEventListener('click', closeMenu);
  };
  setTimeout(() => document.addEventListener('click', closeMenu), 0);
}

function escapeHtml(str) {
  if (!str) return '';
  return str.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;').replace(/'/g, '&#039;');
}

function editDemoNote(cell, filePath) {
  if (cell.querySelector('input')) return;
  const d = state.demos.find(x => x.filePath === filePath);
  const currentNote = d ? (d.note || '') : '';

  cell.innerHTML = `<input type="text" class="note-input" value="${escapeHtml(currentNote)}" placeholder="Введите заметку...">`;
  const input = cell.querySelector('input');
  input.focus();
  input.select();

  let isSaved = false;
  const saveNote = () => {
    if (isSaved) return;
    isSaved = true;
    const newNote = input.value.trim();
    if (d) d.note = newNote;
    cell.innerHTML = newNote ? `<span class="note-text">${escapeHtml(newNote)}</span>` : `<span class="note-placeholder">+ Заметка</span>`;
    postNativeMessage({
      action: 'saveDemoMetadata',
      filePath: filePath,
      map: d ? d.map : '',
      score: d ? d.score : '',
      kd: d ? d.kd : '',
      date: d ? d.date : '',
      note: newNote
    });
  };

  input.addEventListener('keydown', (e) => {
    if (e.key === 'Enter') {
      e.preventDefault();
      saveNote();
    } else if (e.key === 'Escape') {
      e.preventDefault();
      isSaved = true;
      cell.innerHTML = currentNote ? `<span class="note-text">${escapeHtml(currentNote)}</span>` : `<span class="note-placeholder">+ Заметка</span>`;
    }
  });

  input.addEventListener('blur', saveNote);
}

function copyDemoConfig(filePath, btn) {
  postNativeMessage({ action: 'copyDemoConfig', filePath: filePath });

  if (btn) {
    const originalText = btn.textContent;
    btn.textContent = '✓ Скопировано!';
    btn.style.borderColor = '#10b981';
    btn.style.color = '#10b981';
    btn.disabled = true;

    setTimeout(() => {
      btn.textContent = originalText;
      btn.style.borderColor = '';
      btn.style.color = '';
      btn.disabled = false;
    }, 1500);
  }
}
