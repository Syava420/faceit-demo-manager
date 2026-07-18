// App State
const state = {
  activeTab: 'importTab',
  selectedCategory: 'General',
  selectedMapFilter: null,
  demos: [],
  categories: ['General'],
  binds: [],
  settings: {}
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

  ['dragenter', 'dragover', 'dragleave', 'drop'].forEach(eventName => {
    dropZone.addEventListener(eventName, preventDefaults, false);
  });

  function preventDefaults(e) {
    e.preventDefault();
    e.stopPropagation();
  }

  ['dragenter', 'dragover'].forEach(eventName => {
    dropZone.addEventListener(eventName, () => dropZone.classList.add('dragover'), false);
  });

  ['dragleave', 'drop'].forEach(eventName => {
    dropZone.addEventListener(eventName, () => dropZone.classList.remove('dragover'), false);
  });

  dropZone.addEventListener('drop', (e) => {
    const files = [];
    if (e.dataTransfer && e.dataTransfer.files) {
      for (let i = 0; i < e.dataTransfer.files.length; i++) {
        files.push(e.dataTransfer.files[i].path);
      }
    }
    if (files.length > 0) {
      postNativeMessage({ action: 'importFiles', filePaths: files });
    }
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

    item.innerHTML = `
      ${toggleHtml}
      <span class="category-name">📁 ${c.displayName}${nickBadge}</span>
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
      const demoPath = e.dataTransfer.getData('text/demo-filepath');
      const draggedCatPath = e.dataTransfer.getData('text/category-path');

      if (demoPath) {
        postNativeMessage({ action: 'moveDemo', filePath: demoPath, category: c.relativePath });
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
    tr.innerHTML = `
      <td><strong>${d.mapEmoji || '🗺️'} ${d.map || 'Unknown'}</strong></td>
      <td>${d.score || '-'}</td>
      <td>${d.kd || '-'}</td>
      <td>${d.date || '-'}</td>
      <td>${d.note || ''}</td>
      <td>
        <button class="btn-secondary" onclick="event.stopPropagation(); copyDemoConfig('${d.filePath}')">Копировать конфиг</button>
      </td>
    `;
    tr.addEventListener('click', () => {
      document.querySelectorAll('.demo-table tr').forEach(r => r.classList.remove('selected'));
      tr.classList.add('selected');
      state.selectedDemoPath = d.filePath;
    });
    tr.addEventListener('dblclick', () => {
      playSingleDemo(d.filePath);
    });
    tr.addEventListener('dragstart', (e) => {
      e.dataTransfer.setData('text/demo-filepath', d.filePath);
      e.dataTransfer.effectAllowed = 'move';
    });
    elements.tblDemoBody.appendChild(tr);
  });

  if (state.selectedDemoPath && !filtered.some(x => x.filePath === state.selectedDemoPath)) {
    state.selectedDemoPath = null;
  }
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

function copyDemoConfig(filePath) {
  postNativeMessage({ action: 'copyDemoConfig', filePath: filePath });
}
