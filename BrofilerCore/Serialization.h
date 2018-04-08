#pragma once
#include "Common.h"
#include <vector>
#include <sstream>
#include "MemoryPool.h"

#if MT_MSVC_COMPILER_FAMILY
#pragma warning( push )

//C4250. inherits 'std::basic_ostream'
#pragma warning( disable : 4250 )

//C4127. Conditional expression is constant
#pragma warning( disable : 4127 )
#endif

namespace Brofiler
{
	class OutputDataStream : private std::ostringstream 
	{
	public:
		static OutputDataStream Empty;
		// Move constructor rocks!
		// Beware of one copy here(do not use it in performance critical parts)
		std::string GetData();

		// It is important to make private inheritance in order to avoid collision with default operator implementation
		friend OutputDataStream &operator << ( OutputDataStream &stream, const char* val );
		friend OutputDataStream &operator << ( OutputDataStream &stream, int val );
		friend OutputDataStream &operator << ( OutputDataStream &stream, uint64 val );
		friend OutputDataStream &operator << ( OutputDataStream &stream, uint32 val );
		friend OutputDataStream &operator << ( OutputDataStream &stream, int64 val );
		friend OutputDataStream &operator << ( OutputDataStream &stream, char val );
		friend OutputDataStream &operator << ( OutputDataStream &stream, byte val );
		friend OutputDataStream &operator << (OutputDataStream &stream, int8 val);
		friend OutputDataStream &operator << (OutputDataStream &stream, float val);
		friend OutputDataStream &operator << ( OutputDataStream &stream, const std::string& val );
		friend OutputDataStream &operator << ( OutputDataStream &stream, const std::wstring& val );
	};

	template<class T>
	OutputDataStream& operator<<(OutputDataStream &stream, const std::vector<T>& val)
	{
		stream << (uint32)val.size();

		for(auto it = val.begin(); it != val.end(); ++it)
		{
			const T& element = *it;
			stream << element;
		}

		return stream;
	}

	template<class T, uint32 N>
	OutputDataStream& operator<<(OutputDataStream &stream, const MemoryPool<T, N>& val)
	{
		stream << (uint32)val.Size();

		val.ForEach([&](const T& data)
		{
			stream << data;
		});

		return stream;
	}

	////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	class InputDataStream : private std::stringstream {
	public:
		bool CanRead() { return !eof(); }

		InputDataStream();

		void Append(const char *buffer, size_t length);
		bool Skip(size_t length);
		size_t Length();

		template<class T>
		bool Peek(T& data)
		{
			if (Length() < sizeof(T))
				return false;

			pos_type currentPos = tellg();
			read((char*)&data, sizeof(T));
			seekg(currentPos);
			return true;
		}

		template<class T>
		bool Read(T& data)
		{
			if (Length() < sizeof(T))
				return false;

			read((char*)&data, sizeof(T));
			return true;
		}

		friend InputDataStream &operator >> ( InputDataStream &stream, byte &val );
		friend InputDataStream &operator >> (InputDataStream &stream, int16 &val);
		friend InputDataStream &operator >> (InputDataStream &stream, uint16 &val);
		friend InputDataStream &operator >> ( InputDataStream &stream, int32 &val );
		friend InputDataStream &operator >> ( InputDataStream &stream, uint32 &val );
		friend InputDataStream &operator >> ( InputDataStream &stream, int64 &val );
		friend InputDataStream &operator >> ( InputDataStream &stream, uint64 &val );
	};


}

#if MT_MSVC_COMPILER_FAMILY
#pragma warning( pop )
#endif
