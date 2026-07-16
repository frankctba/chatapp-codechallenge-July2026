let connection = null;
let currentUsername = null;
let currentRoom = null;
let soundEnabled = localStorage.getItem('chatapp.soundEnabled') !== 'false';
let audioContext = null;

updateSoundButton();

function ensureAudioContext() {
  // Must be created (or resumed) as a direct result of a user gesture, or browsers
  // leave it permanently suspended - so this only ever gets called from a click handler.
  if (!audioContext) {
    audioContext = new (window.AudioContext || window.webkitAudioContext)();
  }
  if (audioContext.state === 'suspended') {
    audioContext.resume();
  }
  return audioContext;
}

// ---------- Auth ----------

// Mirrors ChatApp.Web/Validation/CredentialValidator.cs and RoomNameValidator.cs.
// This is purely for instant feedback - the server re-checks everything and is the
// actual source of truth.
const USERNAME_PATTERN = /^[a-zA-Z][a-zA-Z0-9._-]{2,19}$/;
const PASSWORD_PATTERN = /^(?=.*[A-Za-z])(?=.*\d).{8,}$/;
const ROOM_PATTERN = /^[a-zA-Z0-9._-]{1,30}$/;

function validateCredentialsLocally(username, password) {
  if (!USERNAME_PATTERN.test(username)) {
    return 'Username: 3-20 characters, starting with a letter (letters, numbers, ".", "_", "-" after that).';
  }
  if (!PASSWORD_PATTERN.test(password)) {
    return 'Password: at least 8 characters, including a letter and a number.';
  }
  return null;
}

async function register() {
  ensureAudioContext();

  const username = document.getElementById('username').value.trim();
  const password = document.getElementById('password').value;

  const localError = validateCredentialsLocally(username, password);
  if (localError) {
    showLoginError(localError);
    return;
  }

  const res = await fetch('/api/auth/register', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ username, password })
  });
  if (!res.ok) {
    showLoginError(await describeAuthError(res));
    return;
  }
  await login();
}

async function login() {
  ensureAudioContext(); // must happen synchronously within the click handler, not after an await

  const username = document.getElementById('username').value.trim();
  const password = document.getElementById('password').value;
  const room = document.getElementById('room').value.trim() || 'general';
  if (!username || !password) {
    showLoginError('Enter a username and password.');
    return;
  }
  if (!ROOM_PATTERN.test(room)) {
    showLoginError('Room: 1-30 characters (letters, numbers, ".", "_", "-").');
    return;
  }

  const res = await fetch('/api/auth/login', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ username, password })
  });
  if (!res.ok) {
    showLoginError(res.status === 401 ? 'Invalid username or password.' : await describeAuthError(res));
    return;
  }

  currentUsername = username;
  currentRoom = room;
  document.getElementById('login').style.display = 'none';
  document.getElementById('chat').style.display = 'flex';
  document.getElementById('whoami').textContent = username;
  await connectToHub();
}

async function describeAuthError(res) {
  if (res.status === 429) {
    return 'Too many attempts - please wait a minute and try again.';
  }
  const text = await res.text();
  return text || `Request failed (${res.status}).`;
}

function showLoginError(text) {
  document.getElementById('loginError').textContent = text;
}

// ---------- SignalR ----------

async function connectToHub() {
  connection = new signalR.HubConnectionBuilder()
    .withUrl('/hubs/chat?room=' + encodeURIComponent(currentRoom))
    .withAutomaticReconnect()
    .build();

  connection.on('MessageHistory', (roomName, messages) => {
    document.getElementById('roomName').textContent = roomName;
    const list = document.getElementById('messages');
    list.innerHTML = '';
    if (messages.length === 0) {
      showEmptyState();
    } else {
      messages.forEach((m) => appendMessage(m, false));
    }
  });

  connection.on('ReceiveMessage', (m) => appendMessage(m, true));
  connection.on('RateLimited', showRateLimitBanner);

  connection.onreconnecting(() => setStatus('reconnecting'));
  connection.onreconnected(() => setStatus('connected'));
  connection.onclose(() => setStatus('disconnected'));

  setStatus('connecting');
  await connection.start();
  setStatus('connected');
  document.getElementById('messageInput').focus();
}

function setStatus(state) {
  const dot = document.getElementById('statusDot');
  const text = document.getElementById('statusText');
  dot.className = 'status-dot ' + state;
  const labels = { connecting: 'connecting…', connected: 'connected', reconnecting: 'reconnecting…', disconnected: 'disconnected' };
  text.textContent = labels[state] || state;
  updateComposerEnabled();
}

function updateComposerEnabled() {
  const connected = document.getElementById('statusDot').classList.contains('connected');
  document.getElementById('sendBtn').disabled = !connected;
  document.getElementById('messageInput').disabled = !connected;
}

let rateLimitBannerTimeout = null;
function showRateLimitBanner(text) {
  const banner = document.getElementById('rateLimitBanner');
  banner.textContent = text;
  banner.hidden = false;
  clearTimeout(rateLimitBannerTimeout);
  rateLimitBannerTimeout = setTimeout(() => { banner.hidden = true; }, 4000);
}

// ---------- Rendering ----------

function showEmptyState() {
  const list = document.getElementById('messages');
  const li = document.createElement('li');
  li.className = 'empty-state';
  li.textContent = 'No messages yet — say hi, or try /stock=aapl.us';
  li.id = 'emptyState';
  list.appendChild(li);
}

function appendMessage(m, isLive) {
  const list = document.getElementById('messages');
  const emptyState = document.getElementById('emptyState');
  if (emptyState) emptyState.remove();

  const isOwn = m.username === currentUsername;
  const isBot = !!m.isBot;

  const row = document.createElement('li');
  row.className = 'msg-row' + (isOwn ? ' own' : '') + (isBot ? ' bot' : '');

  if (!isBot) {
    const avatar = document.createElement('div');
    avatar.className = 'avatar';
    avatar.style.background = colorForUsername(m.username);
    avatar.textContent = initials(m.username);
    avatar.setAttribute('aria-hidden', 'true');
    row.appendChild(avatar);
  }

  const wrap = document.createElement('div');
  wrap.className = 'bubble-wrap';

  const meta = document.createElement('div');
  meta.className = 'meta';
  const time = new Date(m.timestampUtc).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
  meta.textContent = `${isOwn ? 'You' : m.username} · ${time}`;

  const bubble = document.createElement('div');
  bubble.className = 'bubble';
  bubble.textContent = (isBot ? '' : '') + m.content;
  if (isBot) {
    const icon = document.createElement('span');
    icon.className = 'bot-icon';
    icon.textContent = '📈';
    bubble.prepend(icon);
  }

  wrap.appendChild(meta);
  wrap.appendChild(bubble);
  row.appendChild(wrap);
  list.appendChild(row);
  list.scrollTop = list.scrollHeight;

  if (isLive && !isOwn) {
    playChime();
  }
}

function initials(username) {
  return username.slice(0, 2).toUpperCase();
}

function colorForUsername(username) {
  let hash = 0;
  for (let i = 0; i < username.length; i++) {
    hash = username.charCodeAt(i) + ((hash << 5) - hash);
  }
  const hue = Math.abs(hash) % 360;
  return `hsl(${hue}, 45%, 45%)`;
}

// ---------- Room switching ----------

function openRoomSwitcher() {
  const input = document.getElementById('switchRoomInput');
  input.value = currentRoom;
  document.getElementById('switchRoomError').textContent = '';
  document.getElementById('roomSwitcher').hidden = false;
  input.focus();
  input.select();
}

function closeRoomSwitcher() {
  document.getElementById('roomSwitcher').hidden = true;
}

async function switchRoom() {
  const input = document.getElementById('switchRoomInput');
  const newRoom = input.value.trim();
  if (!ROOM_PATTERN.test(newRoom)) {
    document.getElementById('switchRoomError').textContent = 'Room: 1-30 characters (letters, numbers, ".", "_", "-").';
    return;
  }

  closeRoomSwitcher();
  if (newRoom === currentRoom) return;

  if (connection) {
    await connection.stop();
  }
  currentRoom = newRoom;
  document.getElementById('messages').innerHTML = '';
  await connectToHub();
}

// ---------- Sending ----------

async function sendMessage() {
  const input = document.getElementById('messageInput');
  const content = input.value.trim();
  if (!content || !connection) return;
  input.value = '';
  try {
    await connection.invoke('SendMessage', content);
  } catch (err) {
    console.error('Failed to send message', err);
  }
}

// ---------- Sound notification ----------
// Generated in-browser via the Web Audio API - no external audio file needed.

function playChime() {
  if (!soundEnabled || !audioContext) return;
  try {
    if (audioContext.state === 'suspended') {
      audioContext.resume();
    }
    const now = audioContext.currentTime;

    [880, 1175].forEach((freq, i) => {
      const osc = audioContext.createOscillator();
      const gain = audioContext.createGain();
      osc.type = 'sine';
      osc.frequency.value = freq;
      const start = now + i * 0.09;
      gain.gain.setValueAtTime(0, start);
      gain.gain.linearRampToValueAtTime(0.15, start + 0.02);
      gain.gain.exponentialRampToValueAtTime(0.001, start + 0.22);
      osc.connect(gain).connect(audioContext.destination);
      osc.start(start);
      osc.stop(start + 0.24);
    });
  } catch (err) {
    console.warn('Could not play notification sound', err);
  }
}

function toggleSound() {
  soundEnabled = !soundEnabled;
  localStorage.setItem('chatapp.soundEnabled', String(soundEnabled));
  if (soundEnabled) ensureAudioContext();
  updateSoundButton();
}

function updateSoundButton() {
  const btn = document.getElementById('soundToggle');
  if (!btn) return;
  btn.textContent = soundEnabled ? '🔔' : '🔕';
  btn.setAttribute('aria-pressed', String(soundEnabled));
  btn.title = soundEnabled ? 'Mute notification sound' : 'Unmute notification sound';
}