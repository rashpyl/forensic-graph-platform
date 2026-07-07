<template>
  <div id="app-layout">
    <header class="topbar">
      <span class="topbar-title">🔍 Crime Graph Editor</span>
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
  height: 44px;
  background: #1a1d27;
  border-bottom: 1px solid #2d3148;
  display: flex;
  align-items: center;
  padding: 0 20px;
  flex-shrink: 0;
}

.topbar-title {
  font-size: 1rem;
  font-weight: 600;
  letter-spacing: 0.02em;
  color: #a5b4fc;
}

.main-content {
  flex: 1;
  overflow: hidden;
  display: flex;
}
</style>
