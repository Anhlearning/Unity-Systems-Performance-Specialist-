# 🚀 Unity Systems & Performance Specialist Roadmap

> Goal: Trở thành Unity Systems & Performance Specialist  
> - Làm chủ Memory & GC  
> - Tối ưu performance production  
> - Thiết kế system scale-ready  
> - Áp dụng Data-Oriented Thinking  
> - Xây nền tảng để scale indie studio  

---

# 🎯 Long-Term Vision

Sau 6–12 tháng:

- GC = 0 trong gameplay loop
- Hiểu sâu memory & allocation
- Có framework gameplay riêng
- Có benchmark chứng minh năng lực
- Có nền tảng để phát triển studio

---

# 🗺️ 6-Month Learning Plan Overview

| Phase | Focus | Outcome |
|-------|-------|---------|
| 1 | Memory & GC | Zero allocation gameplay loop |
| 2 | Data-Oriented Thinking | Hiểu cache & batch processing |
| 3 | System Architecture | Framework reusable |
| 4 | Tooling | Tăng tốc production |

---

# 🧠 PHASE 1 — Memory & GC Mastery (Week 1–8)

## 🎯 Outcome
- Đọc Profiler thành thạo
- Hiểu allocation đến từ đâu
- GC = 0 trong gameplay loop

---

## 📅 Week 1–2 — Profiler Deep Dive

### 📚 Study
- Unity Profiler Manual  
  https://docs.unity3d.com/Manual/Profiler.html
- GC Best Practices  
  https://docs.unity3d.com/Manual/performance-garbage-collection-best-practices.html

### 🧪 Practice
- Tạo scene 1000 cube di chuyển
- Bật Profiler (CPU + Memory)
- Quan sát:
  - GC.Alloc
  - Timeline View
  - Call Stack

### ✅ Done When
- Biết GC.Alloc nằm ở đâu
- Biết đọc Hierarchy & Timeline

---

## 📅 Week 3–4 — GC Hunting Lab

### 📚 Study
- Jackson Dunstan  
  https://www.jacksondunstan.com/
- C# allocation basics (boxing, closure, lambda)

### 🧪 Practice

Tạo 2000 entity update với 3 version:

1. ❌ Naive (LINQ, string concat, new trong Update)
2. ⚠️ Optimized OOP
3. ✅ Zero Allocation

### 📊 Measure
- GC.Alloc
- Frame time stability

### ✅ Done When
- GC.Alloc = 0 trong gameplay loop
- Chạy 5 phút không spike

---

## 📅 Week 5–6 — Allocation Deep Understanding

### 📚 Study
- Stack vs Heap
- Value type vs Reference type
- Boxing / Unboxing
- String intern
- Struct vs Class tradeoff

### 🧪 Practice
Benchmark:
- 100k struct update
- 100k class update
- Interface call vs direct call

Record:
- CPU time
- Memory usage

---

## 📅 Week 7–8 — Object Pooling Production

### 📚 Study
- Object Pool Pattern  
  https://gameprogrammingpatterns.com/object-pool.html

### 🧪 Practice
- Viết Generic Object Pool
- Projectile Pool
- UI Element Pool

### ✅ Done When
- Không còn Instantiate/Destroy trong gameplay loop
- Pool reuse ổn định

---

# ⚙️ PHASE 2 — Data-Oriented Thinking (Week 9–16)

## 🎯 Outcome
- Hiểu cache locality
- Hiểu batch processing
- Biết khi nào dùng Jobs/Burst

---

## 📅 Week 9–10 — Batch Processing

Refactor:
- 1000 MonoBehaviour Update()

Sang:
- 1 Manager xử lý List<Data>

Compare:
- CPU usage
- Scalability

---

## 📅 Week 11–12 — Cache & Struct Layout

### 📚 Study
- CPU Cache fundamentals
- Catlike Coding  
  https://catlikecoding.com/

### 🧪 Practice
Compare:
- Array of Struct
- Struct of Arrays

Measure performance.

---

## 📅 Week 13–14 — Unity Job System

### 📚 Study
- Unity Job System  
  https://docs.unity3d.com/Manual/JobSystem.html

### 🧪 Practice
- Viết Movement Job
- Viết AI Update Job

Benchmark với version OOP.

---

## 📅 Week 15–16 — Burst Compiler

### 📚 Study
- Burst Documentation  
  https://docs.unity3d.com/Packages/com.unity.burst/

### 🧪 Benchmark
So sánh:
- Normal C#
- Job
- Job + Burst

Record:
- Frame time
- CPU usage

---

# 🏗️ PHASE 3 — System Architecture (Week 17–24)

## 🎯 Outcome
- Framework gameplay reusable
- Code maintainable 3–4 năm

---

## 📅 Week 17–18 — State Machine

### 📚 Study
- State Pattern  
  https://gameprogrammingpatterns.com/state.html

### 🧪 Practice
- Viết State Machine không phụ thuộc MonoBehaviour

---

## 📅 Week 19–20 — Event Bus

### 📚 Study
- Observer Pattern  
  https://gameprogrammingpatterns.com/observer.html

### 🧪 Practice
- Event system không memory leak
- Test subscribe/unsubscribe

---

## 📅 Week 21–22 — Ability System

Viết:
- Ability base class
- Cooldown
- Effect system
- Data-driven config (ScriptableObject)

---

## 📅 Week 23–24 — Save/Load Robust

Viết:
- Serializable data layer
- Version tolerant save
- JSON/Binary compare

---

# 🛠️ PHASE 4 — Tooling (Song song từ Week 18+)

## 📚 Study
- EditorWindow  
  https://docs.unity3d.com/ScriptReference/EditorWindow.html
- Custom Inspector
- SerializedProperty

## 🧪 Practice
- Wave generator tool
- Balance curve editor
- Config validator
- Build checker tool

---

# 🔥 Specialist Daily Mindset

Khi code, luôn hỏi:

- Cái này allocate không?
- Nếu 10x entity thì sao?
- Có cache miss không?
- Có thể batch không?
- Có thể reuse không?

---

# 🏁 After 6 Months

Bạn sẽ:

- Không sợ GC spike
- Hiểu memory sâu
- Có benchmark thực tế
- Có framework gameplay riêng
- Sẵn sàng build indie scale-ready
- Có nền tảng mở studio

---

# 📌 Rule of This Journey

- 30% đọc
- 70% benchmark & đo lường
- Không đo = không học
- Không benchmark = không phải specialist
