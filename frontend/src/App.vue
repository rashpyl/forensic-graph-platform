<template>
  <div id="app-layout">
    <header class="topbar">
      <span class="topbar-title">🔍 Forensic Graph Platform</span>
      <nav class="topbar-actions">
        <button class="nav-btn" disabled title="Coming in the next chunk">
          <span class="nav-icon">＋</span> New Event
        </button>
        <button class="nav-btn" disabled title="Coming in the next chunk">
          <span class="nav-icon">👤</span> New Person
        </button>
        <button class="nav-btn" disabled title="Coming later">
          <span class="nav-icon">ℹ</span> About
        </button>
      </nav>
    </header>
    <main class="main-content">
      <RouterView />
    </main>
  </div>
</template>

<style>
*, *::before, *::after { box-sizing: border-box; margin: 0; padding: 0; }

body {
  font-family: 'Segoe UI', system-ui, sans-serif;
  background: #0f1117;
  color: #e2e8f0;
  height: 100vh;
  overflow: hidden;
}

#app-layout {
  display: flex;
  flex-direction: column;
  height: 100vh;
}

.topbar {
  height: 52px;
  background: #1a1d27;
  border-bottom: 1px solid #2d3148;
  display: flex;
  align-items: center;
  padding: 0 20px;
  flex-shrink: 0;
  gap: 24px;
}

.topbar-title {
  font-size: 1rem;
  font-weight: 600;
  letter-spacing: 0.02em;
  color: #a5b4fc;
}

.topbar-actions {
  display: flex;
  gap: 8px;
  margin-left: auto;
}

.nav-btn {
  background: #232735;
  color: #e2e8f0;
  border: 1px solid #2d3148;
  padding: 6px 12px;
  border-radius: 6px;
  font-size: 0.85rem;
  cursor: pointer;
  display: inline-flex;
  align-items: center;
  gap: 6px;
  transition: background 0.15s;
}

.nav-btn:hover:not(:disabled) {
  background: #2d3148;
}

.nav-btn:disabled {
  opacity: 0.55;
  cursor: not-allowed;
}

.nav-icon {
  font-size: 0.95rem;
  line-height: 1;
}

.main-content {
  flex: 1;
  overflow: hidden;
  display: flex;
}
</style>
